﻿using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using Skimur.Messaging;
using Skimur.Web.Models;
using Skimur.Web.Mvc;
using Subs;
using Subs.Commands;
using Subs.ReadModel;

namespace Skimur.Web.Controllers
{
    [SkimurAuthorize]
    public class SubBansController : BaseController
    {
        private readonly ISubUserBanDao _subUserBanDao;
        private readonly IPermissionDao _permissionDao;
        private readonly ISubDao _subDao;
        private readonly IUserContext _userContext;
        private readonly ISubUserBanWrapper _subUserBanWrapper;
        private readonly ICommandBus _commandBus;

        public SubBansController(ISubUserBanDao subUserBanDao, 
            IPermissionDao permissionDao,
            ISubDao subDao,
            IUserContext userContext,
            ISubUserBanWrapper subUserBanWrapper,
            ICommandBus commandBus)
        {
            _subUserBanDao = subUserBanDao;
            _permissionDao = permissionDao;
            _subDao = subDao;
            _userContext = userContext;
            _subUserBanWrapper = subUserBanWrapper;
            _commandBus = commandBus;
        }
        
        [HttpGet]
        public ActionResult Bans(string subName, string userName, int? pageNumber)
        {
            if (string.IsNullOrEmpty(subName))
                return Redirect(Url.Subs());

            var sub = _subDao.GetSubByName(subName);

            if (sub == null)
                throw new HttpException(404, "sub not found");

            if (!_permissionDao.CanUserManageSubAccess(_userContext.CurrentUser, sub.Id))
                throw new HttpException(403, "not allowed to moderate bans");

            if (!pageNumber.HasValue)
                pageNumber = 1;

            var bannedUsers = _subUserBanDao.GetBannedUsersInSub(sub.Id, userName, (pageNumber.Value - 1)*5, 5);

            var model = new BannedUsersFromSub();
            model.Sub = sub;
            model.Users = new PagedList<SubUserBanWrapped>(_subUserBanWrapper.Wrap(bannedUsers), pageNumber.Value, 5, bannedUsers.HasMore);
            model.Query = userName;

            return View(model);
        }

        [HttpPost, Ajax]
        public ActionResult Ban(string subName, BanUserModel model)
        {
            if (string.IsNullOrEmpty(subName))
                return Redirect(Url.Subs());

            var sub = _subDao.GetSubByName(subName);

            if (sub == null)
                throw new HttpException(404, "sub not found");

            if (!_permissionDao.CanUserManageSubAccess(_userContext.CurrentUser, sub.Id))
                throw new HttpException(403, "not allowed to moderate bans");
            
            var response = _commandBus.Send<BanUserFromSub, BanUserFromSubResponse>(new BanUserFromSub
            {
                UserName = model.UserName,
                BannedBy = _userContext.CurrentUser.Id,
                SubId = sub.Id,
                DateBanned = Common.CurrentTime(),
                ReasonPrivate = model.ReasonPrivate,
                ReasonPublic = model.ReasonPublic
            });

            return CommonJsonResult(response.Error);
        }

        [Ajax]
        public ActionResult UnBan(string subName, string userName)
        {
            if (string.IsNullOrEmpty(subName))
                return Redirect(Url.Subs());

            var sub = _subDao.GetSubByName(subName);

            if (sub == null)
                throw new HttpException(404, "sub not found");

            if (!_permissionDao.CanUserManageSubAccess(_userContext.CurrentUser, sub.Id))
                throw new HttpException(403, "not allowed to moderate bans");
            
            var response = _commandBus.Send<UnBanUserFromSub, UnBanUserFromSubResponse>(new UnBanUserFromSub
            {
                SubId = sub.Id,
                UserName = userName,
                UnBannedBy = _userContext.CurrentUser.Id
            });

            return CommonJsonResult(response.Error);
        }

        [Ajax, HttpPost]
        public ActionResult UpdateBan(string subName, string userName, string reason)
        {
            if (string.IsNullOrEmpty(subName))
                return Redirect(Url.Subs());

            var sub = _subDao.GetSubByName(subName);

            if (sub == null)
                throw new HttpException(404, "sub not found");

            if (!_permissionDao.CanUserManageSubAccess(_userContext.CurrentUser, sub.Id))
                throw new HttpException(403, "not allowed to moderate bans");
            
            var response = _commandBus.Send<UpdateUserSubBan, UpdateUserSubBanResponse>(new UpdateUserSubBan
            {
                UserName = userName,
                UpdatedBy = _userContext.CurrentUser.Id,
                SubId = sub.Id,
                ReasonPrivate = reason,
            });

            if (!string.IsNullOrEmpty(response.Error))
                return CommonJsonResult(response.Error);

            var bannedUser = _subUserBanDao.GetBannedUserInSub(sub.Id, response.UserId);

            return Json(new
            {
                success = true,
                error = (string)null,
                html = RenderView("_Ban", _subUserBanWrapper.Wrap(new List<SubUserBan> {bannedUser})[0])
            });
        }
    }
}
