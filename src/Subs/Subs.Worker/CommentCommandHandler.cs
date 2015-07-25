﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Membership;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using Skimur.Markdown;
using Subs.Commands;
using Subs.Events;
using Subs.Services;

namespace Subs.Worker
{
    public class CommentCommandHandler : 
        ICommandHandlerResponse<CreateComment, CreateCommentResponse>, 
        ICommandHandlerResponse<EditComment, EditCommentResponse>,
        ICommandHandlerResponse<DeleteComment, DeleteCommentResponse>
    {
        private readonly IPostService _postService;
        private readonly IMembershipService _membershipService;
        private readonly ICommentService _commentService;
        private readonly IMarkdownCompiler _markdownCompiler;
        private readonly ICommandBus _commandBus;
        private readonly IPermissionService _permissionService;
        private readonly IEventBus _eventBus;

        public CommentCommandHandler(IPostService postService, 
            IMembershipService membershipService, 
            ICommentService commentService, 
            IMarkdownCompiler markdownCompiler,
            ICommandBus commandBus,
            IPermissionService permissionService,
            IEventBus eventBus)
        {
            _postService = postService;
            _membershipService = membershipService;
            _commentService = commentService;
            _markdownCompiler = markdownCompiler;
            _commandBus = commandBus;
            _permissionService = permissionService;
            _eventBus = eventBus;
        }

        public CreateCommentResponse Handle(CreateComment command)
        {
            var response = new CreateCommentResponse();

            try
            {
                if (string.IsNullOrEmpty(command.Body))
                {
                    response.Error = "A comment is required.";
                    return response;
                }

                command.Body = command.Body.Trim();

                var post = _postService.GetPostBySlug(command.PostSlug);

                if (post == null)
                {
                    response.Error = "Invalid post.";
                    return response;
                }

                var user = _membershipService.GetUserByUserName(command.AuthorUserName);

                if (user == null)
                {
                    response.Error = "Invalid user.";
                    return response;
                }

                Comment parentComment = null;

                if (command.ParentId.HasValue)
                {
                    // this is a reply to a comment.
                    parentComment = _commentService.GetCommentById(command.ParentId.Value);

                    if (parentComment.PostSlug != post.Slug)
                    {
                        // NOTE: this shouldn't happen, and we may want to log it in the future.
                        response.Error = "Replying to a comment in a different post.";
                        return response;
                    }
                }

                var comment = new Comment
                {
                    Id = GuidUtil.NewSequentialId(),
                    DateCreated = command.DateCreated,
                    SubName = post.SubName,
                    ParentId = parentComment != null ? parentComment.Id : (Guid?) null,
                    Parents = new Guid[0],
                    AuthorUserName = user.UserName,
                    AuthorIpAddress = command.AuthorIpAddress,
                    PostSlug = post.Slug,
                    Body = command.Body,
                    BodyFormatted = _markdownCompiler.Compile(command.Body),
                    SendReplies = command.SendReplies,
                    VoteUpCount = 1
                };

                _commentService.InsertComment(comment);
                _commandBus.Send(new CastVoteForComment { DateCasted = post.DateCreated, IpAddress = command.AuthorIpAddress, CommentId = comment.Id, UserName = user.UserName, VoteType = VoteType.Up });
                
                response.CommentId = comment.Id;
                response.Body = comment.Body;
                response.FormattedBody = comment.BodyFormatted;
            }
            catch (Exception ex)
            {
                response.Error = ex.Message;
            }

            return response;
        }

        public EditCommentResponse Handle(EditComment command)
        {
            var response = new EditCommentResponse();

            try
            {
                if (string.IsNullOrEmpty(command.Body))
                {
                    response.Error = "A comment is required.";
                    return response;
                }

                command.Body = command.Body.Trim();

                var comment = _commentService.GetCommentById(command.CommentId);

                if (comment == null)
                {
                    response.Error = "Invalid comment.";
                    return response;
                }

                var bodyFormatted = _markdownCompiler.Compile(command.Body);
                _commentService.UpdateCommentBody(comment.Id, command.Body, bodyFormatted, command.DateEdited);

                response.Body = command.Body;
                response.FormattedBody = bodyFormatted;
            }
            catch (Exception ex)
            {
                response.Error = ex.Message;
            }

            return response;
        }

        public DeleteCommentResponse Handle(DeleteComment command)
        {
            var response = new DeleteCommentResponse();

            try
            {
                var comment = _commentService.GetCommentById(command.CommentId);

                if (comment == null)
                {
                    response.Error = "Invalid comment.";
                    return response;
                }

                var user = _membershipService.GetUserByUserName(command.UserName);

                if (user == null)
                {
                    response.Error = "Invalid user.";
                    return response;
                }

                if (!_permissionService.CanUserDeleteComment(user.UserName, comment))
                {
                    response.Error = "You are not allowed to delete this comment.";
                    return response;
                }

                _commentService.DeleteComment(comment.Id, command.DateDeleted);

                _eventBus.Publish(new CommentDeleted
                {
                    CommentId = comment.Id,
                    PostSlug = comment.PostSlug,
                    SubName = comment.SubName,
                    DeletedByUserName = user.UserName
                });
            }
            catch (Exception ex)
            {
                response.Error = ex.Message;
            }

            return response;
        }
    }
}