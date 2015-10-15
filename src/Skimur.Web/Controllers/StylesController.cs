﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Infrastructure;
using Infrastructure.Messaging;
using Skimur.Web.Models;
using Skimur.Web.Mvc;
using Subs;
using Subs.Commands;
using Subs.ReadModel;
using Subs.Services;

namespace Skimur.Web.Controllers
{
    [SkimurAuthorize]
    public class StylesController : BaseController
    {
        private readonly IPermissionDao _permissionDao;
        private readonly IUserContext _userContext;
        private readonly ISubDao _subDao;
        private readonly ISubCssDao _subStylesDao;
        private readonly IMapper _mapper;
        private readonly ICommandBus _commandBus;

        public StylesController(IPermissionDao permissionDao,
            IUserContext userContext,
            ISubDao subDao,
            ISubCssDao subStylesDao,
            IMapper mapper,
            ICommandBus commandBus)
        {
            _permissionDao = permissionDao;
            _userContext = userContext;
            _subDao = subDao;
            _subStylesDao = subStylesDao;
            _mapper = mapper;
            _commandBus = commandBus;
        }

        public ActionResult Edit(string subName)
        {
            var sub = _subDao.GetSubByName(subName);
            if (sub == null) throw new NotFoundException();

            if (!_permissionDao.CanUserManageSubStyles(_userContext.CurrentUser, sub.Id)) throw new UnauthorizedException();

            var model = new StylesEditModel();
            model.Sub = sub;

            if (Request.Cookies.AllKeys.Contains("preview-" + sub.Name.ToLower()))
            {
                var preview = Session["preview-" + sub.Name.ToLower()] as StylesPreviewModel;
                if (preview != null)
                {
                    _mapper.Map(preview, model);
                    return View(model);
                }
            }

            var styles = _subStylesDao.GetStylesForSub(sub.Id);
            if (styles == null)
            {
                styles = new SubCss();
                styles.CssType = CssType.None;
            }
            
            _mapper.Map(styles, model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ParameterBasedOnFormName("save", "save"), ParameterBasedOnFormName("preview", "preview")]
        public ActionResult Edit(string subName, StylesEditModel model, bool save, bool preview)
        {
            var sub = _subDao.GetSubByName(subName);
            if (sub == null) throw new NotFoundException();

            model.Sub = sub;

            if (!ModelState.IsValid)
                return View(model);

            if (!_permissionDao.CanUserManageSubStyles(_userContext.CurrentUser, sub.Id)) throw new UnauthorizedException();

            if (save)
            {
                var response = _commandBus.Send<EditSubStylesCommand, EditSubStylesCommandResponse>(new EditSubStylesCommand
                {
                    EditedByUserId = _userContext.CurrentUser.Id,
                    SubId = sub.Id,
                    CssType = model.CssType,
                    Embedded = model.Embedded,
                    ExternalCss = model.ExternalCss,
                    GitHubCssProjectName = model.GitHubCssProjectName,
                    GitHubCssProjectTag = model.GitHubCssProjectTag,
                    GitHubLessProjectName = model.GitHubLessProjectName,
                    GitHubLessProjectTag = model.GitHubLessProjectTag
                });

                if (string.IsNullOrEmpty(response.Error))
                {
                    AddSuccessMessage("The styles have succesfully been updated.");
                    CancelPreview(sub.Name, string.Empty); // this will clearly any preview session that may be going on
                }
                else
                {
                    AddErrorMessage(response.Error);
                }
            }
            else if (preview)
            {
                Session["preview-" + sub.Name.ToLower()] = _mapper.Map<StylesEditModel, StylesPreviewModel>(model);
                Response.Cookies.Add(new HttpCookie("preview-" + sub.Name.ToLower()));
            }

            return View(model);
        }

        public ActionResult CancelPreview(string subName, string returnUrl)
        {
            if (!string.IsNullOrEmpty(subName))
            {
                subName = subName.ToLower();
                Session["preview-" + subName] = null;
                Response.Cookies.Add(new HttpCookie("preview-" + subName)
                {
                    Expires = DateTime.Now.AddDays(-1)
                });
            }
            
            return RedirectToLocal(returnUrl);
        }

        public ActionResult SubStyles(string subName)
        {
            var sub = _subDao.GetSubByName(subName);
            if (sub == null) return Content("");

            if (Request.Cookies.AllKeys.Contains("preview-" + sub.Name.ToLower()))
            {
                var preview = Session["preview-" + sub.Name.ToLower()] as StylesPreviewModel;
                if (preview != null)
                {
                    ViewBag.IsPreview = true;
                    ViewBag.PreviewStylesSubName = sub.Name;
                    return PartialView("RenderStyles", _mapper.Map<StylesPreviewModel, SubCss>(preview));
                }
            }
            
            var styles = _subStylesDao.GetStylesForSub(sub.Id);
            if (styles == null) return Content("");

            ViewBag.IsPreview = false;
            return PartialView("RenderStyles", styles);
        }
    }
}
