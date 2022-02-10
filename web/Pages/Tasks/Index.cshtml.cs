﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Atlas_Web.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace Atlas_Web.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly Atlas_WebContext _context;
        private readonly IConfiguration _config;

        public IndexModel(Atlas_WebContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public class UndocumentedReports
        {
            public int ReportObjectId { get; set; }
            public string ModifiedBy { get; set; }
            public string Name { get; set; }
            public string ReportType { get; set; }
            public int Runs { get; set; }
            public string LastMaintained { get; set; }
            public string LastRun { get; set; }
            public string Favorite { get; set; }
        }

        public class NextMaint
        {
            public int ReportId { get; set; }
            public string Date { get; set; }
            public string Name { get; set; }
            public string User { get; set; }
        }

        public class RecommendRetireReports
        {
            public string Name { get; set; }
            public DateTime? MaintenanceDate { get; set; }
            public string MaintenanceDateString { get; set; }
            public int ReportId { get; set; }
            public string Comment { get; set; }
            public string FullName { get; set; }
        }

        public class EditedOutsideAnalyticsData
        {
            public string ReportUrl { get; set; }
            public string LastMod { get; set; }
            public string Author { get; set; }
            public string ModifiedBy { get; set; }
            public string Name { get; set; }
            public string ReportType { get; set; }
            public string Epic { get; set; }
            public string RunReportUrl { get; set; }
            public string EditReportUrl { get; set; }
            public string RecordViewerUrl { get; set; }
            public int Runs { get; set; }
        }

        public class CanMakeReports
        {
            public string Name { get; set; }
            public int? UserId { get; set; }
            public string Role { get; set; }
            public int? RoleId { get; set; }
        }

        public class DeadData
        {
            public string ReportUrl { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string ModifiedBy { get; set; }
            public string LastMod { get; set; }
            public string Server { get; set; }
            public string MasterFile { get; set; }
            public string EpicId { get; set; }
        }

        public IEnumerable<RecommendRetireReports> RecommendRetire { get; set; }
        public IEnumerable<NextMaint> NextMaintenance { get; set; }
        public List<NextMaint> AuditOnly { get; set; }
        public List<NextMaint> MissingSchedule { get; set; }
        public IEnumerable<UndocumentedReports> Undocumented { get; set; }
        public IEnumerable<UndocumentedReports> NewUndocumented { get; set; }
        public IEnumerable<EditedOutsideAnalyticsData> EditedOutsideAnalytics { get; set; }
        public IEnumerable<DeadData> Dead { get; set; }
        public List<AdList> AdLists { get; set; }

        public IActionResult OnGet()
        {
            AdLists = new List<AdList>
            {
                new AdList { Url = "/Users?handler=SharedObjects", Column = 2 },
                new AdList { Url = "/?handler=RecentReports", Column = 2 },
                new AdList { Url = "/?handler=RecentTerms", Column = 2 },
                new AdList { Url = "/?handler=RecentInitiatives", Column = 2 },
                new AdList { Url = "/?handler=RecentCollections", Column = 2 }
            };
            ViewData["AdLists"] = AdLists;
            return Page();
        }

        public async Task<IActionResult> OnGetCanMakeReports()
        {
            var Id = new[]
            {
                "100623",
                "100624",
                "100612",
                "5087102001",
                "5087101001",
                "5087107002"
            };
            ViewData["CanMakeReports"] = await (
                from u in _context.UserGroupsMemberships
                where Id.Contains(u.Group.EpicId)
                select new CanMakeReports
                {
                    Name = u.User.Fullname_Cust,
                    UserId = u.UserId,
                    Role = u.Group.GroupName,
                    RoleId = u.GroupId
                }
            ).ToListAsync();
            //return Partial((".+?"));
            return new PartialViewResult()
            {
                ViewName = "Partials/_CanMakeReports",
                ViewData = ViewData
            };
        }

        public async Task<IActionResult> OnGetRecommendRetire()
        {
            ViewData["RecommendRetire"] = await (
                from l in _context.ReportObjectDocMaintenanceLogs
                join m in _context.MaintenanceLogs on l.MaintenanceLogId equals m.MaintenanceLogId
                where
                    m.MaintenanceLogStatus.MaintenanceLogStatusName == "Recommend Retire"
                    && l.ReportObject.ExecutiveVisibilityYn == "Y"
                select new RecommendRetireReports
                {
                    FullName = m.Maintainer.Fullname_Cust,
                    Name = l.ReportObject.ReportObject.DisplayName,
                    MaintenanceDate = m.MaintenanceDate,
                    MaintenanceDateString = m.MaintenanceDateDisplayString,
                    ReportId = l.ReportObjectId,
                    Comment = m.Comment
                }
            ).ToListAsync();

            //return Partial((".+?"));
            return new PartialViewResult()
            {
                ViewName = "Partials/_RecommendRetire",
                ViewData = ViewData
            };
        }

        public async Task<IActionResult> OnGetUnused()
        {
            ViewData["Dead"] = await (
                from r in _context.ReportObjects.Where(
                    x =>
                        (
                            x.ReportObjectTypeId == 3
                            || x.ReportObjectTypeId == 17
                            || x.ReportObjectTypeId == 20
                            || x.ReportObjectTypeId == 28
                        )
                        && x.DefaultVisibilityYn == "Y"
                        && x.OrphanedReportObjectYn == "N"
                )
                join d in _context.ReportObjectRunData
                    on r.ReportObjectId equals d.ReportObjectId
                    into dta
                from l in dta.DefaultIfEmpty()
                where l.ReportObjectId == null
                join doc in _context.ReportObjectDocs
                    on r.ReportObjectId equals doc.ReportObjectId
                    into doc_dta
                from bla in doc_dta.DefaultIfEmpty()
                where (bla.Hidden ?? "N") == "N" || bla.Hidden == null
                where r.LastModifiedDate < DateTime.Now.AddMonths(-2) || r.LastModifiedDate == null
                orderby r.LastModifiedDate ascending
                select new DeadData
                {
                    ReportUrl = "\\Reports?id=" + r.ReportObjectId,
                    Name = r.DisplayName,
                    Type = r.ReportObjectType.Name,
                    ModifiedBy = r.LastModifiedByUser.Fullname_Cust,
                    LastMod = r.LastUpdatedDateDisplayString,
                    Server = r.SourceServer,
                    MasterFile = r.EpicMasterFile,
                    EpicId = r.EpicRecordId.ToString()
                }
            ).Take(30).ToListAsync();

            //return Partial((".+?"));
            return new PartialViewResult() { ViewName = "Partials/_Unused", ViewData = ViewData };
        }

        public async Task<IActionResult> OnGetMaintRequired()
        {
            DateTime Today = DateTime.Now;
            ViewData["NextMaintenance"] = await (
                from n in (
                    from d in _context.ReportObjectDocs
                    where
                        d.MaintenanceScheduleId != 5
                        && d.MaintenanceScheduleId != null
                        && d.ReportObject.DefaultVisibilityYn == "Y"
                        && d.ReportObject.OrphanedReportObjectYn == "N"
                    join l in (
                        from l in _context.MaintenanceLogs
                        join m in _context.ReportObjectDocMaintenanceLogs
                            on l.MaintenanceLogId equals m.MaintenanceLogId
                        group m by m.ReportObjectId into grp
                        select new
                        {
                            ReportObjectId = grp.Key,
                            MaintenanceLogId = grp.Max(x => x.MaintenanceLogId)
                        }
                    )
                        on d.ReportObjectId equals l.ReportObjectId
                        into tmp
                    from t in tmp.DefaultIfEmpty()
                    join m in _context.MaintenanceLogs
                        on t.MaintenanceLogId equals m.MaintenanceLogId
                        into tmptwo
                    from ttwo in tmptwo.DefaultIfEmpty()
                    select new
                    {
                        d.ReportObjectId,
                        NextDate = d.MaintenanceScheduleId == 1
                            ? (ttwo.MaintenanceDate ?? d.LastUpdateDateTime ?? Today).AddMonths(3)
                            : // quarterly
                              d.MaintenanceScheduleId == 2
                                ? (ttwo.MaintenanceDate ?? d.LastUpdateDateTime ?? Today).AddMonths(
                                      6
                                  )
                                : // twice a year
                                  d.MaintenanceScheduleId == 3
                                    ? (
                                          ttwo.MaintenanceDate ?? d.LastUpdateDateTime ?? Today
                                      ).AddYears(1)
                                    : // yearly
                                      d.MaintenanceScheduleId == 4
                                        ? (
                                              ttwo.MaintenanceDate ?? d.LastUpdateDateTime ?? Today
                                          ).AddYears(2)
                                        : // every two years
                                          (
                                              ttwo.MaintenanceDate
                                              ?? d.LastUpdateDateTime
                                              ?? d.CreatedDateTime
                                              ?? Today
                                          ),
                        Name = d.ReportObject.DisplayName,
                        LastUser = (
                            ttwo.Maintainer.Fullname_Cust != "user not found"
                                ? ttwo.Maintainer.Fullname_Cust
                                : d.UpdatedByNavigation.Fullname_Cust
                        )
                    }
                )
                where n.NextDate < Today.AddMonths(2)
                orderby n.NextDate
                select new NextMaint
                {
                    ReportId = n.ReportObjectId,
                    Date = n.NextDate.ToString("MM/dd/yyyy"),
                    Name = n.Name,
                    User = n.LastUser
                }
            ).ToListAsync();

            //return Partial((".+?"));
            return new PartialViewResult()
            {
                ViewName = "Partials/_MaintRequired",
                ViewData = ViewData
            };
        }

        public async Task<IActionResult> OnGetAudit()
        {
            DateTime Today = DateTime.Now;
            ViewData["AuditOnly"] = await (
                from n in (
                    from d in _context.ReportObjectDocs
                    where
                        d.MaintenanceScheduleId == 5
                        && d.ReportObject.DefaultVisibilityYn == "Y"
                        && d.ReportObject.OrphanedReportObjectYn == "N"
                    join l in (
                        from l in _context.MaintenanceLogs
                        join m in _context.ReportObjectDocMaintenanceLogs
                            on l.MaintenanceLogId equals m.MaintenanceLogId
                        group m by m.ReportObjectId into grp
                        select new
                        {
                            ReportObjectId = grp.Key,
                            MaintenanceLogId = grp.Max(x => x.MaintenanceLogId)
                        }
                    )
                        on d.ReportObjectId equals l.ReportObjectId
                        into tmp
                    from t in tmp.DefaultIfEmpty()
                    join m in _context.MaintenanceLogs
                        on t.MaintenanceLogId equals m.MaintenanceLogId
                        into tmptwo
                    from ttwo in tmptwo.DefaultIfEmpty()
                    select new
                    {
                        d.ReportObjectId,
                        NextDate = d.MaintenanceScheduleId == 1
                            ? (ttwo.MaintenanceDate ?? d.LastUpdateDateTime ?? Today).AddMonths(3)
                            : // quarterly
                              d.MaintenanceScheduleId == 2
                                ? (ttwo.MaintenanceDate ?? d.LastUpdateDateTime ?? Today).AddMonths(
                                      6
                                  )
                                : // twice a year
                                  d.MaintenanceScheduleId == 3
                                    ? (
                                          ttwo.MaintenanceDate ?? d.LastUpdateDateTime ?? Today
                                      ).AddYears(1)
                                    : // yearly
                                      d.MaintenanceScheduleId == 4
                                        ? (
                                              ttwo.MaintenanceDate ?? d.LastUpdateDateTime ?? Today
                                          ).AddYears(2)
                                        : // every two years
                                          (
                                              ttwo.MaintenanceDate
                                              ?? d.LastUpdateDateTime
                                              ?? d.CreatedDateTime
                                              ?? Today
                                          ),
                        Name = d.ReportObject.DisplayName,
                        LastUser = (
                            ttwo.Maintainer.Fullname_Cust != "user not found"
                                ? ttwo.Maintainer.Fullname_Cust
                                : d.UpdatedByNavigation.Fullname_Cust
                        )
                    }
                )
                where n.NextDate < Today.AddMonths(2)
                orderby n.NextDate
                select new NextMaint
                {
                    ReportId = n.ReportObjectId,
                    Date = n.NextDate.ToString("MM/dd/yyyy"),
                    Name = n.Name,
                    User = n.LastUser
                }
            ).ToListAsync();

            //return Partial((".+?"));
            return new PartialViewResult() { ViewName = "Partials/_Audit", ViewData = ViewData };
        }

        public async Task<IActionResult> OnGetNoSchedule()
        {
            DateTime Today = DateTime.Now;
            ViewData["MissingSchedule"] = await (
                from n in (
                    from d in _context.ReportObjectDocs
                    where
                        d.MaintenanceScheduleId == null
                        && d.ReportObject.DefaultVisibilityYn == "Y"
                        && d.ReportObject.OrphanedReportObjectYn == "N"
                    join l in (
                        from l in _context.MaintenanceLogs
                        join m in _context.ReportObjectDocMaintenanceLogs
                            on l.MaintenanceLogId equals m.MaintenanceLogId
                        group m by m.ReportObjectId into grp
                        select new
                        {
                            ReportObjectId = grp.Key,
                            MaintenanceLogId = grp.Max(x => x.MaintenanceLogId)
                        }
                    )
                        on d.ReportObjectId equals l.ReportObjectId
                        into tmp
                    from t in tmp.DefaultIfEmpty()
                    join m in _context.MaintenanceLogs
                        on t.MaintenanceLogId equals m.MaintenanceLogId
                        into tmptwo
                    from ttwo in tmptwo.DefaultIfEmpty()
                    select new
                    {
                        d.ReportObjectId,
                        NextDate = (ttwo.MaintenanceDate ?? d.LastUpdateDateTime ?? Today),
                        Name = d.ReportObject.DisplayName,
                        LastUser = (
                            ttwo.Maintainer.Fullname_Cust != "user not found"
                                ? ttwo.Maintainer.Fullname_Cust
                                : d.UpdatedByNavigation.Fullname_Cust
                        )
                    }
                )
                where n.NextDate < Today.AddMonths(2)
                orderby n.NextDate
                select new NextMaint
                {
                    ReportId = n.ReportObjectId,
                    Date = n.NextDate.ToString("MM/dd/yyyy"),
                    Name = n.Name,
                    User = n.LastUser
                }
            ).ToListAsync();

            //return Partial((".+?"));
            return new PartialViewResult()
            {
                ViewName = "Partials/_NoSchedule",
                ViewData = ViewData
            };
        }

        public async Task<IActionResult> OnGetNotAnalytics()
        {
            ViewData["EditedOutsideAnalytics"] = await (
                from r in _context.ReportObjects
                where
                    r.LastModifiedDate > DateTime.Today.AddMonths(-6)
                    && r.DefaultVisibilityYn == "Y"
                    && r.OrphanedReportObjectYn == "N"
                    && (r.ReportObjectTypeId == 17 || r.ReportObjectTypeId == 3)
                join l in _context.UserRoleLinks on r.LastModifiedByUserId equals l.UserId into tmp
                from t in tmp.DefaultIfEmpty()
                where ((int?)t.UserRolesId ?? 2) != 1
                join l in _context.UserRoleLinks on r.AuthorUserId equals l.UserId into tmptwo
                from ttwp in tmptwo.DefaultIfEmpty()
                where ((int?)ttwp.UserRolesId ?? 2) != 1
                join f in (
                    from tr in _context.ReportObjectTopRuns
                    group tr by tr.ReportObjectId into g
                    select new { ReportObjectId = g.Key, Cnt = g.Count() }
                )
                    on r.ReportObjectId equals f.ReportObjectId
                orderby r.ReportObjectId
                select new EditedOutsideAnalyticsData
                {
                    ReportUrl = "\\Reports?id=" + r.ReportObjectId.ToString(),
                    LastMod = r.LastUpdatedDateDisplayString,
                    Author = r.AuthorUser.Fullname_Cust,
                    ModifiedBy = r.LastModifiedByUser.Fullname_Cust,
                    Name = r.DisplayName,
                    ReportType = r.ReportObjectType.Name,
                    Epic = r.EpicMasterFile + " " + r.EpicRecordId.ToString(),
                    RunReportUrl = Helpers.HtmlHelpers.ReportUrlFromParams(
                        _config["AppSettings:org_domain"],
                        HttpContext,
                        r,
                        _context,
                        User.Identity.Name
                    ),
                    EditReportUrl = Helpers.HtmlHelpers.EditReportFromParams(
                        _config["AppSettings:org_domain"],
                        HttpContext,
                        r.ReportServerPath,
                        r.SourceServer,
                        r.EpicMasterFile,
                        r.EpicReportTemplateId.ToString(),
                        r.EpicRecordId.ToString()
                    ),
                    RecordViewerUrl = Helpers.HtmlHelpers.RecordViewerLink(
                        _config["AppSettings:org_domain"],
                        HttpContext,
                        r.EpicMasterFile,
                        r.EpicRecordId.ToString()
                    ),
                    Runs = ((int?)f.Cnt ?? 0)
                }
            ).ToListAsync();

            //return Partial((".+?"));
            return new PartialViewResult()
            {
                ViewName = "Partials/_NotAnalytics",
                ViewData = ViewData
            };
        }

        public ActionResult OnGetTopUndocumented()
        {
            var rpts = new int[] { 17, 28, 3, 20 };
            ViewData["Undocumented"] = (
                from r in _context.ReportObjects
                join t in _context.ReportObjectTypes
                    on r.ReportObjectTypeId equals t.ReportObjectTypeId
                where rpts.Contains(r.ReportObjectTypeId ?? 0) && r.DefaultVisibilityYn == "Y"
                select new
                {
                    r.ReportObjectId,
                    ModifiedBy = (
                        r.LastModifiedByUser.Fullname_Cust != "user not found"
                            ? r.LastModifiedByUser.Fullname_Cust
                            : r.AuthorUser.Fullname_Cust
                    ),
                    Name = r.DisplayName,
                    ReportType = (
                        t.Name == "Reporting Workbench Report"
                            ? "Workbench"
                            : t.Name == "Source Radar Dashboard"
                                ? "Dashboard"
                                : t.Name == "Epic-Crystal Report"
                                    ? "Crystal"
                                    : "SSRS"
                    ),
                    Runs = r.ReportObjectRunData.Count,
                    LastMaintained = (r.LastModifiedDate ?? DateTime.Today.AddYears(-1)),
                    LastRun = (r.ReportObjectRunData.Max(x => x.RunStartTime) ?? DateTime.Now),
                    Favs = (
                        from f in _context.UserFavorites
                        where f.ItemType.ToLower() == "report" && f.ItemId == r.ReportObjectId
                        select new { f.ItemId }
                    ).FirstOrDefault()
                } into tmp
                join o in _context.ReportObjectDocs
                    on tmp.ReportObjectId equals o.ReportObjectId
                    into rs
                from p in rs.DefaultIfEmpty()
                where p.DeveloperDescription == null
                orderby tmp.Runs descending
                select new UndocumentedReports
                {
                    ReportObjectId = tmp.ReportObjectId,
                    ModifiedBy = tmp.ModifiedBy,
                    Name = tmp.Name,
                    ReportType = tmp.ReportType,
                    Runs = tmp.Runs,
                    Favorite = tmp.Favs == null ? "" : "Yes",
                    LastMaintained = tmp.LastMaintained.ToString("MM/dd/yyyy"),
                    LastRun = tmp.LastRun.ToString("MM/dd/yyyy")
                }
            ).Take(60).ToList();

            //return Partial((".+?"));
            return new PartialViewResult()
            {
                ViewName = "Partials/_TopUndocumented",
                ViewData = ViewData
            };
        }

        public ActionResult OnGetNewUndocumented()
        {
            var rpts = new int[] { 17, 28, 3, 20 };
            ViewData["NewUndocumented"] = (
                from r in _context.ReportObjects
                join t in _context.ReportObjectTypes
                    on r.ReportObjectTypeId equals t.ReportObjectTypeId
                where
                    rpts.Contains(r.ReportObjectTypeId ?? 0)
                    && r.DefaultVisibilityYn == "Y"
                    && r.LastModifiedDate > DateTime.Today.AddMonths(-1)
                select new
                {
                    r.ReportObjectId,
                    ModifiedBy = (
                        r.LastModifiedByUser.Fullname_Cust != "user not found"
                            ? r.LastModifiedByUser.Fullname_Cust
                            : r.AuthorUser.Fullname_Cust
                    ),
                    Name = r.DisplayName,
                    ReportType = (
                        t.Name == "Reporting Workbench Report"
                            ? "Workbench"
                            : t.Name == "Source Radar Dashboard"
                                ? "Dashboard"
                                : t.Name == "Epic-Crystal Report"
                                    ? "Crystal"
                                    : "SSRS"
                    ),
                    Runs = r.ReportObjectRunData.Count,
                    LastMaintained = (r.LastModifiedDate ?? DateTime.Today.AddYears(-1)),
                    LastRun = (r.ReportObjectRunData.Max(x => x.RunStartTime) ?? DateTime.Now),
                    Favs = (
                        from f in _context.UserFavorites
                        where f.ItemType.ToLower() == "report" && f.ItemId == r.ReportObjectId
                        select new { f.ItemId }
                    ).FirstOrDefault()
                } into tmp
                join o in _context.ReportObjectDocs
                    on tmp.ReportObjectId equals o.ReportObjectId
                    into rs
                from p in rs.DefaultIfEmpty()
                where p.DeveloperDescription == null
                orderby tmp.Runs descending
                select new UndocumentedReports
                {
                    ReportObjectId = tmp.ReportObjectId,
                    ModifiedBy = tmp.ModifiedBy,
                    Name = tmp.Name,
                    ReportType = tmp.ReportType,
                    Runs = tmp.Runs,
                    Favorite = tmp.Favs == null ? "" : "Yes",
                    LastMaintained = tmp.LastMaintained.ToString("MM/dd/yyyy"),
                    LastRun = tmp.LastRun.ToString("MM/dd/yyyy")
                }
            ).Take(60).ToList();

            //return Partial((".+?"));
            return new PartialViewResult()
            {
                ViewName = "Partials/_NewUndocumented",
                ViewData = ViewData
            };
        }
    }
}
