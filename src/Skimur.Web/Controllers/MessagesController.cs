﻿using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using Skimur.Logging;
using Skimur.Messaging;
using Skimur.Web.Infrastructure;
using Skimur.Web.Services;
using Skimur.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Skimur.App;
using Skimur.App.Commands;
using Skimur.App.ReadModel;

namespace Skimur.Web.Controllers
{
    [Authorize]
    public class MessagesController : BaseController
    {
        private readonly ICommandBus _commandBus;
        private readonly ILogger<MessagesController> _logger;
        private readonly IUserContext _userContext;
        private readonly IMessageDao _messageDao;
        private readonly IMessageWrapper _messageWrapper;
        private readonly ISubDao _subDao;
        private readonly IModerationDao _moderationDao;

        public MessagesController(ICommandBus commandBus,
            ILogger<MessagesController> logger,
            IUserContext userContext,
            IMessageDao messageDao,
            IMessageWrapper messageWrapper,
            ISubDao subDao,
            IModerationDao moderationDao)
        {
            _commandBus = commandBus;
            _logger = logger;
            _userContext = userContext;
            _messageDao = messageDao;
            _messageWrapper = messageWrapper;
            _subDao = subDao;
            _moderationDao = moderationDao;
        }

        public ActionResult Inbox(InboxType type, int? pageNumber, int? pageSize)
        {
            ViewBag.ManageNavigationKey = "inbox";

            if (pageNumber == null || pageNumber < 1)
                pageNumber = 1;
            if (pageSize == null)
                pageSize = 25;
            if (pageSize > 100)
                pageSize = 100;
            if (pageSize < 1)
                pageSize = 1;

            var skip = (pageNumber - 1) * pageSize;
            var take = pageSize;

            SeekedList<Guid> messages;

            switch (type)
            {
                case InboxType.All:
                    messages = _messageDao.GetAllMessagesForUser(_userContext.CurrentUser.Id, skip, take);
                    break;
                case InboxType.Unread:
                    messages = _messageDao.GetUnreadMessagesForUser(_userContext.CurrentUser.Id, skip, take);
                    break;
                case InboxType.Messages:
                    messages = _messageDao.GetPrivateMessagesForUser(_userContext.CurrentUser.Id, skip, take);
                    break;
                case InboxType.CommentReplies:
                    messages = _messageDao.GetCommentRepliesForUser(_userContext.CurrentUser.Id, skip, take);
                    break;
                case InboxType.PostReplies:
                    messages = _messageDao.GetPostRepliesForUser(_userContext.CurrentUser.Id, skip, take);
                    break;
                case InboxType.Mentions:
                    messages = _messageDao.GetMentionsForUser(_userContext.CurrentUser.Id, skip, take);
                    break;
                default:
                    throw new Exception("Unknown inbox type");
            }

            var model = new InboxViewModel();
            model.InboxType = type;
            model.IsModerator = _moderationDao.GetSubsModeratoredByUser(_userContext.CurrentUser.Id).Count > 0;
            model.Messages = new PagedList<MessageWrapped>(_messageWrapper.Wrap(messages, _userContext.CurrentUser), pageNumber.Value, pageSize.Value, messages.HasMore);

            return View(model);
        }

        public ActionResult Compose(string to = null, string subject = null, string message = null)
        {
            ViewBag.ManageNavigationKey = "compose";

            ModelState.Clear(); // prevent query string from overriding our model properties

            var model = new ComposeMessageViewModel();
            model.To = to;
            model.Subject = subject;
            model.Message = message;
            model.IsModerator = _moderationDao.GetSubsModeratoredByUser(_userContext.CurrentUser.Id).Count > 0;

            return View(model);
        }

        public ActionResult Sent(int? pageNumber, int? pageSize)
        {
            ViewBag.ManageNavigationKey = "sent";

            if (pageNumber == null || pageNumber < 1)
                pageNumber = 1;
            if (pageSize == null)
                pageSize = 25;
            if (pageSize > 100)
                pageSize = 100;
            if (pageSize < 1)
                pageSize = 1;

            var skip = (pageNumber - 1) * pageSize;
            var take = pageSize;

            var messages = _messageDao.GetSentMessagesForUser(_userContext.CurrentUser.Id, skip, take);

            var model = new InboxViewModel();
            model.InboxType = InboxType.Sent;
            model.IsModerator = _moderationDao.GetSubsModeratoredByUser(_userContext.CurrentUser.Id).Count > 0;
            model.Messages = new PagedList<MessageWrapped>(_messageWrapper.Wrap(messages, _userContext.CurrentUser), pageNumber.Value, pageSize.Value, messages.HasMore);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Compose(ComposeMessageViewModel model)
        {
            ViewBag.ManageNavigationKey = "compose";
            model.IsModerator = _moderationDao.GetSubsModeratoredByUser(_userContext.CurrentUser.Id).Count > 0;

            if (!ModelState.IsValid)
                return View(model);

            SendMessageResponse response = null;

            try
            {
                response = _commandBus.Send<SendMessage, SendMessageResponse>(new SendMessage
                {
                    To = model.To,
                    Subject = model.Subject,
                    Body = model.Message,
                    Author = _userContext.CurrentUser.Id
                });
            }
            catch (Exception ex)
            {
                _logger.Error("An error occured sending a message.", ex);
                AddErrorMessage("An unknown error occured.");
            }

            if (string.IsNullOrEmpty(response.Error))
            {
                AddSuccessMessage("Your message has been sent.");
            }
            else
            {
                AddErrorMessage(response.Error);
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult Reply(ReplyMessageViewModel model)
        {
            var response = _commandBus.Send<ReplyMessage, ReplyMessageResponse>(new ReplyMessage
            {
                Author = _userContext.CurrentUser.Id,
                AuthorIp = HttpContext.RemoteAddress(),
                ReplyToMessageId = model.ReplyToMessage,
                Body = model.Body
            });

            return CommonJsonResult(response.Error);
        }

        public ActionResult Details(Guid id, Guid? context = null)
        {
            var message = _messageDao.GetMessageById(id);

            // only private messages can be perma linked
            if (message.MessageType != MessageType.Private)
                throw new NotFoundException();

            if (message.FirstMessage.HasValue)
            {
                // the user is looking at the wrong page.
                // the user should never get here
                return Redirect(Url.MessageDetails(message));
            }

            // this will return all the messages for this thread, including the first message that started the conversation
            var messages = _messageDao.GetMessagesForThread(message.Id);

            // let's make sure that the user is involved with at least one of these messages
            if (!_userContext.CurrentUser.IsAdmin)
            {
                var userModeratingSubs =
                    _moderationDao.GetSubsModeratoredByUserWithPermissions(_userContext.CurrentUser.Id)
                        .Where(x => x.Value.HasPermission(ModeratorPermissions.Mail)).Select(x => x.Key).ToList();

                // NOTE: Should we check for the user being involved with any message in the thread, or is the first message enough?

                if (message.ToUser.HasValue && message.ToUser.Value == _userContext.CurrentUser.Id
                    || message.AuthorId == _userContext.CurrentUser.Id
                    || message.FromSub.HasValue && userModeratingSubs.Contains(message.FromSub.Value)
                    || message.ToSub.HasValue && userModeratingSubs.Contains(message.ToSub.Value))
                {
                    // the user is involved in these discussions!
                }
                else
                {
                    throw new UnauthorizedException();
                }
            }

            var model = new MessageThreadViewModel();
            model.IsModerator = _moderationDao.GetSubsModeratoredByUser(_userContext.CurrentUser.Id).Count > 0;
            model.Messages.AddRange(_messageWrapper.Wrap(messages, _userContext.CurrentUser));
            if (context.HasValue)
                model.ContextMessage = model.Messages.SingleOrDefault(x => x.Message.Id == context.Value);
            model.FirstMessage = model.Messages.Single(x => !x.Message.FirstMessage.HasValue);

            return View(model);
        }

        public ActionResult ModeratorMail(InboxType type, string subName = null, int? pageNumber = null, int? pageSize = null)
        {
            ViewBag.ManageNavigationKey = "moderatormail";

            if (pageNumber == null || pageNumber < 1)
                pageNumber = 1;
            if (pageSize == null)
                pageSize = 25;
            if (pageSize > 100)
                pageSize = 100;
            if (pageSize < 1)
                pageSize = 1;

            var skip = (pageNumber - 1) * pageSize;
            var take = pageSize;

            var moderatingSubs = _moderationDao.GetSubsModeratoredByUserWithPermissions(_userContext.CurrentUser.Id);

            var model = new InboxViewModel { InboxType = type };
            model.IsModerator = moderatingSubs.Count > 0;

            if (!string.IsNullOrEmpty(subName))
            {
                var sub = _subDao.GetSubByName(subName);
                if (sub == null) throw new NotFoundException();

                // make the sure that the user is allowed to see this mod mail
                if (!_userContext.CurrentUser.IsAdmin)
                {
                    if (!moderatingSubs.ContainsKey(sub.Id)) throw new UnauthorizedException();
                    if (!moderatingSubs[sub.Id].HasPermission(ModeratorPermissions.Mail)) throw new UnauthorizedException();
                }

                model.Sub = sub;
                model.ModeratorMailForSubs = new List<Guid> { sub.Id };
            }
            else
            {
                model.ModeratorMailForSubs = new List<Guid>();
                foreach (var key in moderatingSubs.Keys)
                {
                    if (moderatingSubs[key].HasPermission(ModeratorPermissions.Mail))
                        model.ModeratorMailForSubs.Add(key);
                }
            }

            SeekedList<Guid> messages;
            if (moderatingSubs.Count == 0)
                messages = new SeekedList<Guid>();
            else
                switch (type)
                {
                    case InboxType.ModeratorMail:
                        messages = _messageDao.GetModeratorMailForSubs(moderatingSubs.Select(x => x.Key).ToList(), skip, take);
                        break;
                    case InboxType.ModeratorMailUnread:
                        messages = _messageDao.GetUnreadModeratorMailForSubs(moderatingSubs.Select(x => x.Key).ToList(), skip, take);
                        break;
                    case InboxType.ModeratorMailSent:
                        messages = _messageDao.GetSentModeratorMailForSubs(moderatingSubs.Select(x => x.Key).ToList(), skip, take);
                        break;
                    default:
                        throw new Exception("invalid type");
                }

            model.Messages = new PagedList<MessageWrapped>(_messageWrapper.Wrap(messages, _userContext.CurrentUser), pageNumber.Value, pageSize.Value, messages.HasMore);

            return View(model);
        }

        [HttpPost]
        public ActionResult MarkMessagesAsRead(List<Guid> messages)
        {
            if (messages == null || messages.Count == 0)
                return CommonJsonResult(true);

            _commandBus.Send(new MarkMessagesAsRead
            {
                UserId = _userContext.CurrentUser.Id,
                Messages = messages
            });

            return CommonJsonResult(true);
        }

        [HttpPost]
        public ActionResult MarkMessagesAsUnread(List<Guid> messages)
        {
            if (messages == null || messages.Count == 0)
                return CommonJsonResult(true);

            _commandBus.Send(new MarkMessagesAsUnread
            {
                UserId = _userContext.CurrentUser.Id,
                Messages = messages
            });

            return CommonJsonResult(true);
        }
    }
}
