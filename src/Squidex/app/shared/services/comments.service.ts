/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import {
    ApiUrlConfig,
    DateTime,
    Model,
    pretifyError,
    Version
} from '@app/framework';

export class CommentsDto extends Model {
    constructor(
        public readonly createdComments: CommentDto[],
        public readonly updatedComments: CommentDto[],
        public readonly deletedComments: string[],
        public readonly version: Version
    ) {
        super();
    }
}

export class CommentDto extends Model {
    constructor(
        public readonly id: string,
        public readonly time: DateTime,
        public readonly text: string,
        public readonly user: string
    ) {
        super();
    }

    public with(value: Partial<CommentDto>): CommentDto {
        return this.clone(value);
    }
}

export class UpsertCommentDto {
    constructor(
        public readonly text: string
    ) {
    }
}

@Injectable()
export class CommentsService {
    constructor(
        private readonly http: HttpClient,
        private readonly apiUrl: ApiUrlConfig
    ) {
    }

    public getComments(appName: string, commentsId: string, version: Version): Observable<CommentsDto> {
        const url = this.apiUrl.buildUrl(`api/apps/${appName}/comments/${commentsId}?version=${version.value}`);

        return this.http.get<any>(url).pipe(
                map(response => {
                    return new CommentsDto(
                        response.createdComments.map((item: any) => {
                            return new CommentDto(
                                item.id,
                                DateTime.parseISO_UTC(item.time),
                                item.text,
                                item.user);
                        }),
                        response.updatedComments.map((item: any) => {
                            return new CommentDto(
                                item.id,
                                DateTime.parseISO_UTC(item.time),
                                item.text,
                                item.user);
                        }),
                        response.deletedComments,
                        new Version(response.version)
                    );
                }),
                pretifyError('Failed to load comments.'));
    }

    public postComment(appName: string, commentsId: string, dto: UpsertCommentDto): Observable<CommentDto> {
        const url = this.apiUrl.buildUrl(`api/apps/${appName}/comments/${commentsId}`);

        return this.http.post(url, dto).pipe(
                map(response => {
                    const body: any = response;

                    return new CommentDto(
                        body.id,
                        DateTime.parseISO_UTC(body.time),
                        body.text,
                        body.user);
                }),
                pretifyError('Failed to create comment.'));
    }

    public putComment(appName: string, commentsId: string, commentId: string, dto: UpsertCommentDto): Observable<any> {
        const url = this.apiUrl.buildUrl(`api/apps/${appName}/comments/${commentsId}/${commentId}`);

        return this.http.put(url, dto).pipe(
                pretifyError('Failed to update comment.'));
    }

    public deleteComment(appName: string, commentsId: string, commentId: string): Observable<any> {
        const url = this.apiUrl.buildUrl(`api/apps/${appName}/comments/${commentsId}/${commentId}`);

        return this.http.delete(url).pipe(
                pretifyError('Failed to delete comment.'));
    }
}