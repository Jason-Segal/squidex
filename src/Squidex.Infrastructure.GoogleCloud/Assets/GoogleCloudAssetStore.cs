﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Cloud.Storage.V1;

namespace Squidex.Infrastructure.Assets
{
    public sealed class GoogleCloudAssetStore : IAssetStore, IInitializable
    {
        private static readonly UploadObjectOptions IfNotExists = new UploadObjectOptions { IfGenerationMatch = 0 };
        private static readonly CopyObjectOptions IfNotExistsCopy = new CopyObjectOptions { IfGenerationMatch = 0 };
        private readonly string bucketName;
        private StorageClient storageClient;

        public GoogleCloudAssetStore(string bucketName)
        {
            Guard.NotNullOrEmpty(bucketName, nameof(bucketName));

            this.bucketName = bucketName;
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                storageClient = StorageClient.Create();

                await storageClient.GetBucketAsync(bucketName, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException($"Cannot connect to google cloud bucket '${bucketName}'.", ex);
            }
        }

        public string GeneratePublicUrl(string id, long version, string suffix)
        {
            return null;
        }

        public async Task CopyAsync(string sourceFileName, string id, long version, string suffix, CancellationToken ct = default)
        {
            var objectName = GetObjectName(id, version, suffix);

            try
            {
                await storageClient.CopyObjectAsync(bucketName, sourceFileName, bucketName, objectName, IfNotExistsCopy, ct);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                throw new AssetNotFoundException(sourceFileName, ex);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new AssetAlreadyExistsException(objectName);
            }
        }

        public async Task DownloadAsync(string id, long version, string suffix, Stream stream, CancellationToken ct = default)
        {
            var objectName = GetObjectName(id, version, suffix);

            try
            {
                await storageClient.DownloadObjectAsync(bucketName, objectName, stream, cancellationToken: ct);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                throw new AssetNotFoundException($"Id={id}, Version={version}", ex);
            }
        }

        public Task UploadAsync(string id, long version, string suffix, Stream stream, bool overwrite = false, CancellationToken ct = default)
        {
            return UploadCoreAsync(GetObjectName(id, version, suffix), stream, overwrite, ct);
        }

        public Task UploadAsync(string fileName, Stream stream, CancellationToken ct = default)
        {
            return UploadCoreAsync(fileName, stream, false, ct);
        }

        public Task DeleteAsync(string id, long version, string suffix)
        {
            return DeleteCoreAsync(GetObjectName(id, version, suffix));
        }

        public Task DeleteAsync(string fileName)
        {
            return DeleteCoreAsync(fileName);
        }

        private async Task UploadCoreAsync(string objectName, Stream stream, bool overwrite = false, CancellationToken ct = default)
        {
            try
            {
                await storageClient.UploadObjectAsync(bucketName, objectName, "application/octet-stream", stream, overwrite ? null : IfNotExists, ct);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new AssetAlreadyExistsException(objectName);
            }
        }

        private async Task DeleteCoreAsync(string objectName)
        {
            try
            {
                await storageClient.DeleteObjectAsync(bucketName, objectName);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return;
            }
        }

        private string GetObjectName(string id, long version, string suffix)
        {
            Guard.NotNullOrEmpty(id, nameof(id));

            if (storageClient == null)
            {
                throw new InvalidOperationException("No connection established yet.");
            }

            var name = GetFileName(id, version, suffix);

            return name;
        }

        private static string GetFileName(string id, long version, string suffix)
        {
            return StringExtensions.JoinNonEmpty("_", id, version.ToString(), suffix);
        }
    }
}
