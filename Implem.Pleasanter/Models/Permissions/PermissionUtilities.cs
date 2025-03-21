﻿using Implem.DefinitionAccessor;
using Implem.Libraries.Classes;
using Implem.Libraries.DataSources.Interfaces;
using Implem.Libraries.DataSources.SqlServer;
using Implem.Libraries.Utilities;
using Implem.Pleasanter.Libraries.DataSources;
using Implem.Pleasanter.Libraries.DataTypes;
using Implem.Pleasanter.Libraries.Extensions;
using Implem.Pleasanter.Libraries.General;
using Implem.Pleasanter.Libraries.Html;
using Implem.Pleasanter.Libraries.HtmlParts;
using Implem.Pleasanter.Libraries.Models;
using Implem.Pleasanter.Libraries.Requests;
using Implem.Pleasanter.Libraries.Resources;
using Implem.Pleasanter.Libraries.Responses;
using Implem.Pleasanter.Libraries.Security;
using Implem.Pleasanter.Libraries.Server;
using Implem.Pleasanter.Libraries.Settings;
using Implem.Pleasanter.Libraries.Web;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using static Implem.Pleasanter.Libraries.ServerScripts.ServerScriptModel;
namespace Implem.Pleasanter.Models
{
    public static class PermissionUtilities
    {
        /// <summary>
        /// Fixed:
        /// </summary>
        public static string Permission(Context context, long referenceId)
        {
            var controlId = context.Forms.ControlId();
            var selector = "#" + controlId;
            var itemModel = new ItemModel(
                context: context,
                referenceId: referenceId);
            var siteModel = new SiteModel(
                context: context,
                siteId: itemModel.SiteId,
                formData: context.Forms);
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: referenceId);
            var invalid = PermissionValidators.OnUpdating(
                context: context,
                ss: siteModel.SiteSettings);
            switch (invalid.Type)
            {
                case Error.Types.None: break;
                default: return invalid.MessageJson(context: context);
            }
            var hb = new HtmlBuilder();
            var permissions = SourceCollection(
                context: context,
                ss: siteModel.SiteSettings,
                searchText: context.Forms.Data("SearchPermissionElements"),
                currentPermissions: CurrentPermissions(
                    context: context,
                    referenceId: referenceId));
            var offset = context.Forms.Int("SourcePermissionsOffset");
            switch (controlId)
            {
                case "SourcePermissions":
                    return new ResponseCollection(context: context)
                        .Append(selector, hb.SelectableItems(
                            listItemCollection: permissions
                                .Page(offset)
                                .ListItemCollection(
                                    context: context,
                                    ss: siteModel.SiteSettings,
                                    withType: false)))
                        .Val("#SourcePermissionsOffset", Paging.NextOffset(
                            offset, permissions.Count(), Parameters.Permissions.PageSize)
                                .ToString())
                        .ToJson();
                default:
                    return new ResponseCollection(context: context)
                        .Html(selector + "Editor", hb.Permission(
                            context: context,
                            siteModel: siteModel,
                            referenceId: referenceId,
                            site: itemModel.ReferenceType == "Sites"))
                        .Val("#SourcePermissionsOffset", Parameters.Permissions.PageSize)
                        .RemoveAttr(selector, "data-action")
                        .Invoke("setPermissionEvents")
                        .ToJson();
            }
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static List<Permission> Page(this List<Permission> permissions, int offset)
        {
            return permissions
                .Skip(offset)
                .Take(Parameters.Permissions.PageSize)
                .ToList();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static Dictionary<string, ControlData> ListItemCollection(
            this List<Permission> permissions,
            Context context,
            SiteSettings ss,
            bool withType = true)
        {
            return permissions.ToDictionary(
                o => o.Key(),
                o => o.ControlData(
                    context: context,
                    ss: ss,
                    withType: withType));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder Permission(
            this HtmlBuilder hb, Context context, SiteModel siteModel, long referenceId, bool site)
        {
            var ss = siteModel.SiteSettings;
            var disableRecordPermission = site
                ? false
                : ss.PermissionForUpdating?.Any() == true;
            return hb.FieldSet(
                id: "FieldSetPermissionEditor",
                css: " enclosed",
                legendText: Displays.PermissionSetting(context: context),
                action: () => hb
                    .Inherit(
                        context: context,
                        siteModel: siteModel,
                        site: site)
                    .Div(id: "PermissionEditor", action: () => hb
                        .PermissionEditor(
                            context: context,
                            ss: ss,
                            referenceId: referenceId,
                            disableRecordPermission: disableRecordPermission,
                            _using: !site || siteModel.SiteId == siteModel.InheritPermission))
                    .FieldCheckBox(
                        controlId: "NoDisplayIfReadOnly",
                        fieldCss: "field-auto-thin both",
                        labelText: Displays.NoDisplayIfReadOnly(context: context),
                        _checked: ss.NoDisplayIfReadOnly,
                        _using: site)
                    .FieldCheckBox(
                        controlId: "NotInheritPermissionsWhenCreatingSite",
                        fieldCss: "field-auto-thin",
                        labelText: Displays.NotInheritPermissionsWhenCreatingSite(context: context),
                        _checked: ss.NotInheritPermissionsWhenCreatingSite,
                        _using: ss.ReferenceType == "Sites"));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static HtmlBuilder Inherit(
            this HtmlBuilder hb, Context context, SiteModel siteModel, bool site)
        {
            return site && siteModel.SiteId != 0
                ? hb.FieldDropDown(
                    context: context,
                    controlId: "InheritPermission",
                    fieldCss: "field-auto",
                    controlCss: " auto-postback search",
                    labelText: Displays.InheritPermission(context: context),
                    optionCollection: InheritTargets(
                        context: context,
                        ss: siteModel.SiteSettings).OptionCollection,
                    selectedValue: siteModel.InheritPermission.ToString(),
                    action: "SetPermissions",
                    method: "post")
                : hb;
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static (Dictionary<string, ControlData> OptionCollection, int TotalCount) InheritTargets(
            Context context,
            SiteSettings ss,
            int offset = 0,
            int pageSize = 0,
            string searchText = "")
        {
            Dictionary<string, ControlData> dictionary = new Dictionary<string, ControlData>();
            if (offset == 0)
            {
                dictionary.Add(ss.SiteId.ToString(), new ControlData(Displays.NotInheritPermission(context: context)));
            }
            var (dataRows, totalCount) = InheritTargetsDataRows(
                context: context,
                ss: ss,
                offset: offset,
                pageSize: pageSize,
                searchText: searchText);
            dictionary.AddRange(
                dataRows.ToDictionary(
                    o => o["SiteId"].ToString(),
                    o => new ControlData($"[{o["SiteId"]}] {o["Title"]}")));
            return (dictionary, totalCount);
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static (EnumerableRowCollection<DataRow> DataRows, int TotalCount) InheritTargetsDataRows(
            Context context,
            SiteSettings ss,
            int offset = 0,
            int pageSize = 0,
            string searchText = "")
        {
            var where = Rds.SitesWhere()
                .TenantId(context.TenantId)
                .SiteId(ss.SiteId, _operator: "<>")
                .InheritPermission(raw: "\"Sites\".\"SiteId\"")
                .Add(
                    raw: Def.Sql.CanReadSites,
                    _using: !context.HasPrivilege)
                .SqlWhereLike(
                    tableName: "Sites",
                    name: "SearchText",
                    searchText: searchText,
                    clauseCollection: new List<string>()
                    {
                        Rds.Sites_Title_WhereLike(factory: context),
                        Rds.Sites_SiteId_WhereLike(factory: context)
                    });
            var statements = new List<SqlStatement>()
            {
                Rds.SelectSites(
                    offset: offset,
                    pageSize: pageSize,
                    dataTableName: "Main",
                    column: Rds.SitesColumn()
                        .SiteId()
                        .Title(),
                    join: Rds.SitesJoinDefault(),
                    where: where,
                    orderBy: Rds.SitesOrderBy()
                        .Title()
                        .SiteId()),
                Rds.SelectCount(
                    tableName: "Sites",
                    join: Rds.SitesJoinDefault(),
                    where: where)
            };
            var dataSet = Repository.ExecuteDataSet(
                context: context,
                statements: statements.ToArray());
            var totalCount = Rds.Count(dataSet);
            return (dataSet.Tables["Main"].AsEnumerable(), totalCount);
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder PermissionEditor(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            long referenceId,
            bool _using,
            bool disableRecordPermission)
        {
            var currentPermissions = CurrentCollection(
                context: context,
                referenceId: referenceId);
            var sourcePermissions = SourceCollection(
                context: context,
                ss: ss,
                searchText: context.Forms.Data("SearchPermissionElements"),
                currentPermissions: currentPermissions);
            var offset = context.Forms.Int("PermissionSourceOffset");
            return _using
                ? hb
                    .CurrentPermissions(
                        context: context,
                        ss: ss,
                        permissions: currentPermissions,
                        disableRecordPermission: disableRecordPermission)
                    .SourcePermissions(
                        context: context,
                        ss: ss,
                        permissions: sourcePermissions
                            .Page(offset)
                            .ListItemCollection(
                                context: context,
                                ss: ss,
                                withType: false),
                        offset: offset,
                        totalCount: sourcePermissions.Count(),
                        disableRecordPermission: disableRecordPermission)
                : hb;
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder CurrentPermissions(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            IEnumerable<Permission> permissions,
            bool disableRecordPermission)
        {
            return hb.FieldSelectable(
                controlId: "CurrentPermissions",
                fieldCss: "field-vertical both",
                controlContainerCss: "container-selectable",
                controlCss: " send-all",
                labelText: Displays.Permissions(context: context),
                listItemCollection: permissions.ToDictionary(
                    o => o.Key(), o => o.ControlData(
                        context: context,
                        ss: ss)),
                commandOptionPositionIsTop: true,
                commandOptionAction: () => hb
                    .Div(css: "command-left", action: () => hb
                        .Button(
                            controlId: "OpenPermissionsDialog",
                            controlCss: "button-icon post",
                            text: Displays.AdvancedSetting(context: context),
                            onClick: "$p.openPermissionsDialog($(this));",
                            icon: "ui-icon-gear",
                            action: "OpenPermissionsDialog",
                            method: "post")
                        .Button(
                            disabled: disableRecordPermission,
                            controlId: "DeletePermissions",
                            controlCss: "button-icon post",
                            text: Displays.DeletePermission(context: context),
                            onClick: "$p.setPermissions($(this));",
                            icon: "ui-icon-circle-triangle-e",
                            action: "SetPermissions",
                            method: "delete")));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder SourcePermissions(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            Dictionary<string, ControlData> permissions,
            bool disableRecordPermission,
            int offset = 0,
            int totalCount = 0)
        {
            return hb
                .FieldSelectable(
                    controlId: "SourcePermissions",
                    fieldCss: "field-vertical",
                    controlContainerCss: "container-selectable",
                    controlWrapperCss: " h300",
                    labelText: Displays.OptionList(context: context),
                    listItemCollection: permissions,
                    commandOptionPositionIsTop: true,
                    action: "Permissions",
                    method: "post",
                    commandOptionAction: () => hb
                        .Div(css: "command-left", action: () => hb
                            .Button(
                                disabled: disableRecordPermission,
                                controlId: "AddPermissions",
                                controlCss: "button-icon post",
                                text: Displays.AddPermission(context: context),
                                onClick: "$p.setPermissions($(this));",
                                icon: "ui-icon-circle-triangle-w",
                                action: "SetPermissions",
                                method: "post")
                            .TextBox(
                                controlId: "SearchPermissionElements",
                                controlCss: " auto-postback w100",
                                placeholder: Displays.Search(context: context),
                                action: "SearchPermissionElements",
                                method: "post")
                            .Button(
                                text: Displays.Search(context: context),
                                controlCss: "button-icon",
                                onClick: "$p.send($('#SearchPermissionElements'));",
                                icon: "ui-icon-search")))
                .Hidden(
                    controlId: "SourcePermissionsOffset",
                    css: "always-send",
                    value: Paging.NextOffset(offset, totalCount, Parameters.Permissions.PageSize)
                        .ToString());
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static List<Permission> CurrentCollection(Context context, long referenceId)
        {
            return Repository.ExecuteTable(
                context: context,
                statements: Rds.SelectPermissions(
                    column: Rds.PermissionsColumn()
                        .DeptId()
                        .GroupId()
                        .UserId()
                        .PermissionType(),
                    where: Rds.PermissionsWhere().ReferenceId(referenceId),
                    orderBy: Rds.PermissionsOrderBy()
                        .UserId()
                        .GroupId()
                        .DeptId()))
                            .AsEnumerable()
                            .Select(dataRow => new Permission(dataRow))
                            .Where(o => o.Exists(context: context))
                            .ToList();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static List<Permission> SourceCollection(
            Context context,
            SiteSettings ss,
            string searchText,
            List<Permission> currentPermissions,
            bool allUsers = true,
            int offset = 0)
        {
            var sourceCollection = new List<Permission>();
            if (!context.DisableAllUsersPermission
                && allUsers
                && searchText.IsNullOrEmpty())
            {
                sourceCollection.Add(new Permission(
                    ss: ss,
                    name: "User",
                    id: -1,
                    source: true));
            }
            Repository.ExecuteTable(
                context: context,
                statements: new SqlStatement[]
                {
                    Rds.SelectDepts(
                        column: Rds.DeptsColumn()
                            .DeptId(_as: "Id")
                            .Add(columnBracket: "'Dept' as \"Name\""),
                        where: Rds.DeptsWhere()
                            .TenantId(context.TenantId)
                            .DeptId(_operator: ">0")
                            .Disabled(false)
                            .SqlWhereLike(
                                tableName: "Depts",
                                name: "SearchText",
                                searchText: searchText,
                                clauseCollection: new List<string>()
                                {
                                    Rds.Depts_DeptCode_WhereLike(factory: context),
                                    Rds.Depts_DeptName_WhereLike(factory: context),
                                    Rds.Depts_Body_WhereLike(factory: context)
                                })
                            .OnSelectingWhereExtendedSqls(
                                context: context,
                                ss: ss,
                                extendedSqls: Parameters.ExtendedSqls?.Where(o => o.OnSelectingWherePermissionsDepts))),
                    Rds.SelectGroups(
                        column: Rds.GroupsColumn()
                            .GroupId(_as: "Id")
                            .Add(columnBracket: "'Group' as \"Name\""),
                        where: Rds.GroupsWhere()
                            .TenantId(context.TenantId)
                            .GroupId(_operator: ">0")
                            .Disabled(false)
                            .SqlWhereLike(
                                tableName: "Groups",
                                name: "SearchText",
                                searchText: searchText,
                                clauseCollection: new List<string>()
                                {
                                    Rds.Groups_GroupId_WhereLike(factory: context),
                                    Rds.Groups_GroupName_WhereLike(factory: context),
                                    Rds.Groups_Body_WhereLike(factory: context)
                                })
                            .OnSelectingWhereExtendedSqls(
                                context: context,
                                ss: ss,
                                extendedSqls: Parameters.ExtendedSqls?.Where(o => o.OnSelectingWherePermissionsGroups)),
                        unionType: Sqls.UnionTypes.UnionAll),
                    Rds.SelectUsers(
                        column: Rds.UsersColumn()
                            .UserId(_as: "Id")
                            .Add(columnBracket: "'User' as \"Name\""),
                        join: Rds.UsersJoin()
                            .Add(new SqlJoin(
                                tableBracket: "\"Depts\"",
                                joinType: SqlJoin.JoinTypes.LeftOuter,
                                joinExpression: "\"Users\".\"DeptId\"=\"Depts\".\"DeptId\"")),
                        where: Rds.UsersWhere()
                            .TenantId(context.TenantId)
                            .UserId(_operator: ">0")
                            .Disabled(false)
                            .SqlWhereLike(
                                tableName: "\"Users\"",
                                name: "SearchText",
                                searchText: searchText,
                                clauseCollection: new List<string>()
                                {
                                    Rds.Users_LoginId_WhereLike(factory: context),
                                    Rds.Users_Name_WhereLike(factory: context),
                                    Rds.Users_UserCode_WhereLike(factory: context),
                                    Rds.Users_Body_WhereLike(factory: context),
                                    Rds.Depts_DeptCode_WhereLike(factory: context),
                                    Rds.Depts_DeptName_WhereLike(factory: context),
                                    Rds.Depts_Body_WhereLike(factory: context)
                                })
                            .OnSelectingWhereExtendedSqls(
                                context: context,
                                ss: ss,
                                extendedSqls: Parameters.ExtendedSqls?.Where(o => o.OnSelectingWherePermissionsUsers)),
                        unionType: Sqls.UnionTypes.UnionAll)
                })
                    .AsEnumerable()
                    .ForEach(dataRow =>
                        sourceCollection.Add(
                            new Permission(
                                ss: ss,
                                name: dataRow.String("Name"),
                                id: dataRow.Int("Id"),
                                source: true)));
            return sourceCollection
                .Where(o => !currentPermissions.Any(p => p.NameAndId() == o.NameAndId()))
                .ToList();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string PermissionListItem(
            Context context,
            SiteSettings ss,
            IEnumerable<Permission> permissions,
            IEnumerable<string> selectedValueTextCollection = null,
            bool withType = true)
        {
            return new HtmlBuilder().SelectableItems(
                listItemCollection: permissions.ToDictionary(
                    o => o.Key(), o => o.ControlData(
                        context: context,
                        ss: ss,
                        withType: withType)),
                selectedValueTextCollection: permissions
                    .Where(o => selectedValueTextCollection?.Any(p =>
                        Same(p, o.Key())) == true)
                    .Select(o => o.Key())).ToString();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static List<SqlStatement> InsertStatements(
            Context context,
            SiteSettings ss,
            Dictionary<string, List<int>> columns,
            Dictionary<string, Permissions.Types> permissions,
            long referenceId = 0)
        {
            var insertSet = new List<PermissionModel>();
            permissions?.ForEach(data =>
            {
                switch (data.Key)
                {
                    case "Dept":
                        insertSet.Add(new PermissionModel(
                            context: context,
                            referenceId: referenceId,
                            deptId: context.DeptId,
                            groupId: 0,
                            userId: 0,
                            permissionType: data.Value));
                        break;
                    case "Group":
                        Groups(context: context).ForEach(groupId =>
                            insertSet.Add(new PermissionModel(
                                context: context,
                                referenceId: referenceId,
                                deptId: 0,
                                groupId: groupId,
                                userId: 0,
                                permissionType: data.Value)));
                        break;
                    case "User":
                        insertSet.Add(new PermissionModel(
                            context: context,
                            referenceId: referenceId,
                            deptId: 0,
                            groupId: 0,
                            userId: context.UserId,
                            permissionType: data.Value));
                        break;
                    default:
                        var columnData = columns.FirstOrDefault(o => o.Key.StartsWith(data.Key + ","));
                        if (!columnData.Equals(default(KeyValuePair<string, List<int>>)))
                        {
                            foreach (var id in columnData.Value)
                            {
                                switch (columnData.Key.Split_2nd())
                                {
                                    case "Dept":
                                        var dept = SiteInfo.Dept(
                                            tenantId: context.TenantId,
                                            deptId: id);
                                        if (dept.Id > 0)
                                        {
                                            insertSet.Add(new PermissionModel(
                                                context: context,
                                                referenceId: referenceId,
                                                deptId: dept.Id,
                                                groupId: 0,
                                                userId: 0,
                                                permissionType: data.Value));
                                        }
                                        break;
                                    case "Group":
                                        var group = SiteInfo.Group(
                                            tenantId: context.TenantId,
                                            groupId: id);
                                        if (group.Id > 0)
                                        {
                                            insertSet.Add(new PermissionModel(
                                                context: context,
                                                referenceId: referenceId,
                                                deptId: 0,
                                                groupId: group.Id,
                                                userId: 0,
                                                permissionType: data.Value));
                                        }
                                        break;
                                    case "User":
                                        var user = SiteInfo.User(
                                            context: context,
                                            userId: id);
                                        if (!user.Anonymous())
                                        {
                                            insertSet.Add(new PermissionModel(
                                                context: context,
                                                referenceId: referenceId,
                                                deptId: 0,
                                                groupId: 0,
                                                userId: user.Id,
                                                permissionType: data.Value));
                                        }
                                        break;
                                }
                            }
                        }
                        break;
                }
            });
            return insertSet
                .OrderByDescending(o => o.PermissionType)
                .GroupBy(o => o.DeptId + "," + o.GroupId + "," + o.UserId)
                .Select(o => (SqlStatement)Insert(o.First()))
                .ToList();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static List<SqlStatement> UpdateStatements(            
            Context context,
            SiteSettings ss,
            long referenceId,
            Dictionary<string, List<int>> columns,
            Dictionary<string, Permissions.Types> permissions)
        {
            var statements = new List<SqlStatement>();
            if (permissions?.Any() == true)
            {
                statements.Add(Rds.PhysicalDeletePermissions(
                    where: Rds.PermissionsWhere().ReferenceId(referenceId)));
            }
            statements.AddRange(InsertStatements(
                context: context,
                ss: ss,
                columns: columns,
                permissions: permissions,
                referenceId: referenceId));
            return statements;
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static List<int> Groups(Context context, bool enableOnly = false)
        {
            if (context.Authenticated)
            {
                var sql = enableOnly
                    ? context.Sqls.GetEnabledGroup
                    : context.Sqls.GetGroup;
                return Repository.ExecuteTable(
                    context: context,
                    statements: new SqlStatement(sql))
                        .AsEnumerable()
                        .Select(o => o.Int("GroupId"))
                        .ToList();
            }
            else
            {
                return new List<int>();
            }
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static void UpdatePermissions(
            this List<SqlStatement> statements,
            Context context,
            SiteSettings ss,
            long referenceId,
            IEnumerable<string> permissions,
            bool site = false)
        {
            statements.Add(Rds.PhysicalDeletePermissions(
                where: Rds.PermissionsWhere().ReferenceId(referenceId)));
            if (!site || site && ss.InheritPermission == ss.SiteId)
            {
                new PermissionCollection(
                    context: context,
                    referenceId: referenceId,
                    permissions: permissions)
                        .ForEach(permissionModel =>
                            statements.Add(Insert(permissionModel)));
            }
            if (site)
            {
                statements.Add(StatusUtilities.UpdateStatus(
                    tenantId: context.TenantId,
                    type: StatusUtilities.Types.PermissionsUpdated));
            }
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static SqlInsert Insert(PermissionModel permissionModel)
        {
            return Rds.InsertPermissions(param: Rds.PermissionsParam()
                .ReferenceId(raw: permissionModel.ReferenceId == 0
                    ? Def.Sql.Identity
                    : permissionModel.ReferenceId.ToString())
                .PermissionType(raw: permissionModel.PermissionType.ToLong().ToString())
                .DeptId(raw: permissionModel.DeptId.ToString())
                .GroupId(raw: permissionModel.GroupId.ToString())
                .UserId(raw: permissionModel.UserId.ToString()));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string SetPermissions(Context context, long referenceId)
        {
            var itemModel = new ItemModel(
                context: context,
                referenceId: referenceId);
            var siteModel = new SiteModel(
                context: context,
                siteId: itemModel.SiteId,
                formData: context.Forms);
            var site = itemModel.ReferenceType == "Sites";
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: referenceId);
            var invalid = PermissionValidators.OnUpdating(
                context: context,
                ss: siteModel.SiteSettings);
            switch (invalid.Type)
            {
                case Error.Types.None: break;
                default: return invalid.MessageJson(context: context);
            }
            var res = new ResponseCollection(context: context);
            var selectedCurrentPermissions = context.Forms.List("CurrentPermissions");
            var selectedSourcePermissions = context.Forms.List("SourcePermissions");
            if (context.Forms.ControlId() != "AddPermissions" &&
                selectedCurrentPermissions.Any(o =>
                    o.StartsWith($"User,{context.UserId},")))
            {
                res.Message(Messages.PermissionNotSelfChange(context: context));
            }
            else
            {
                var currentPermissions = context.Forms.Exists("CurrentPermissionsAll")
                    ? Permissions.Get(context.Forms.List("CurrentPermissionsAll"))
                    : CurrentCollection(
                        context: context,
                        referenceId: referenceId);
                switch (context.Forms.ControlId())
                {
                    case "InheritPermission":
                        res.InheritPermission(
                            context: context,
                            itemModel: itemModel,
                            siteModel: siteModel);
                        break;
                    case "AddPermissions":
                        res.AddPermissions(
                            context: context,
                            siteModel: siteModel,
                            selectedSourcePermissions: selectedSourcePermissions,
                            currentPermissions: currentPermissions);
                        break;
                    case "PermissionPattern":
                        res.ReplaceAll(
                            "#PermissionParts",
                            new HtmlBuilder().PermissionParts(
                                context: context,
                                controlId: "PermissionParts",
                                labelText: Displays.Permissions(context: context),
                                permissionType: (Permissions.Types)context.Forms.Long(
                                    "PermissionPattern"),
                                disableRecordPermission: site
                                    ? false
                                    : siteModel.SiteSettings.PermissionForUpdating?.Any() == true));
                        break;
                    case "ChangePermissions":
                        res.ChangePermissions(
                            context: context,
                            ss: siteModel.SiteSettings,
                            selector: "#CurrentPermissions",
                            currentPermissions: currentPermissions,
                            selectedCurrentPermissions: selectedCurrentPermissions,
                            permissionType: GetPermissionTypeByForm(context: context));
                        break;
                    case "DeletePermissions":
                        res.DeletePermissions(
                            context: context,
                            siteModel: siteModel,
                            selectedCurrentPermissions: selectedCurrentPermissions,
                            currentPermissions: currentPermissions);
                        break;
                }
            }
            return res
                .SetMemory("formChanged", true)
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static void InheritPermission(
            this ResponseCollection res,
            Context context,
            ItemModel itemModel,
            SiteModel siteModel)
        {
            var inheritPermission = context.Forms.Long("InheritPermission");
            var hb = new HtmlBuilder();
            if (siteModel.SiteId == inheritPermission)
            {
                hb.PermissionEditor(
                    context: context,
                    ss: siteModel.SiteSettings,
                    referenceId: siteModel.InheritPermission,
                    disableRecordPermission: false,
                    _using:
                        itemModel.ReferenceType != "Sites" ||
                        siteModel.SiteId == inheritPermission);
            }
            res
                .Html("#PermissionEditor", hb)
                .SetData("#InheritPermission")
                .SetData("#CurrentPermissions")
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static void AddPermissions(
            this ResponseCollection res,
            Context context,
            SiteModel siteModel,
            List<string> selectedSourcePermissions,
            List<Permission> currentPermissions)
        {
            currentPermissions.AddRange(
                Permissions.Get(selectedSourcePermissions));
            currentPermissions
                .Where(o => selectedSourcePermissions.Any(p => Same(p, o.Key())))
                .ForEach(o => o.Type = Permissions.General());
            var sourcePermissions = SourceCollection(
                context: context,
                ss: siteModel.SiteSettings,
                searchText: context.Forms.Data("SearchPermissionElements"),
                currentPermissions: currentPermissions);
            res
                .ScrollTop("#SourcePermissionsWrapper")
                .Html("#CurrentPermissions", PermissionListItem(
                    context: context,
                    ss: siteModel.SiteSettings,
                    permissions: currentPermissions,
                    selectedValueTextCollection: selectedSourcePermissions))
                .Html("#SourcePermissions", PermissionListItem(
                    context: context,
                    ss: siteModel.SiteSettings,
                    permissions: sourcePermissions.Page(0),
                    withType: false))
                .Val("#SourcePermissionsOffset", Parameters.Permissions.PageSize)
                .SetData("#CurrentPermissions")
                .SetData("#SourcePermissions");
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static void ChangePermissions(
            this ResponseCollection res,
            Context context,
            SiteSettings ss,
            string selector,
            IEnumerable<Permission> currentPermissions,
            IEnumerable<string> selectedCurrentPermissions,
            Permissions.Types permissionType)
        {
            selectedCurrentPermissions.ForEach(o =>
                currentPermissions
                    .Where(p => Same(p.Key(), o))
                    .First()
                    .Type = permissionType);
            res
                .CloseDialog()
                .Html(selector, PermissionListItem(
                    context: context,
                    ss: ss,
                    permissions: currentPermissions,
                    selectedValueTextCollection: selectedCurrentPermissions))
                .SetData(selector);
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static void DeletePermissions(
            this ResponseCollection res,
            Context context,
            SiteModel siteModel,
            List<string> selectedCurrentPermissions,
            List<Permission> currentPermissions)
        {
            currentPermissions.RemoveAll(o =>
                selectedCurrentPermissions.Any(p => Same(p, o.Key())));
            var sourcePermissions = SourceCollection(
                context: context,
                ss: siteModel.SiteSettings,
                searchText: context.Forms.Data("SearchPermissionElements"),
                currentPermissions: currentPermissions);
            res
                .Html("#CurrentPermissions", PermissionListItem(
                    context: context,
                    ss: siteModel.SiteSettings,
                    permissions: currentPermissions))
                .Html("#SourcePermissions", PermissionListItem(
                    context: context,
                    ss: siteModel.SiteSettings,
                    permissions: sourcePermissions.Page(0),
                    selectedValueTextCollection: selectedCurrentPermissions,
                    withType: false))
                .Val("#SourcePermissionsOffset", Parameters.Permissions.PageSize)
                .SetData("#CurrentPermissions")
                .SetData("#SourcePermissions");
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string SearchPermissionElements(Context context, long referenceId)
        {
            var itemModel = new ItemModel(
                context: context,
                referenceId: referenceId);
            var siteModel = new SiteModel(
                context: context,
                siteId: itemModel.SiteId,
                formData: context.Forms);
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: referenceId);
            var invalid = PermissionValidators.OnUpdating(
                context: context,
                ss: siteModel.SiteSettings);
            switch (invalid.Type)
            {
                case Error.Types.None: break;
                default: return invalid.MessageJson(context: context);
            }
            var res = new ResponseCollection(context: context);
            var currentPermissions = CurrentPermissions(
                context: context,
                referenceId: referenceId);
            var sourcePermissions = SourceCollection(
                context: context,
                ss: siteModel.SiteSettings,
                searchText: context.Forms.Data("SearchPermissionElements"),
                currentPermissions: currentPermissions);
            return res
                .Html("#SourcePermissions", PermissionListItem(
                    context: context,
                    ss: siteModel.SiteSettings,
                    permissions: sourcePermissions.Page(0),
                    selectedValueTextCollection: context.Forms.Data("SourcePermissions")
                        .Deserialize<List<string>>()?
                        .Where(o => o != string.Empty),
                    withType: false))
                .Val("#SourcePermissionsOffset", Parameters.Permissions.PageSize)
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static List<Permission> CurrentPermissions(Context context, long referenceId)
        {
            return context.Forms.Exists("CurrentPermissionsAll")
                ? Permissions.Get(context.Forms.List("CurrentPermissionsAll"))
                : CurrentCollection(
                    context: context,
                    referenceId: referenceId).ToList();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string OpenPermissionsDialog(Context context, long referenceId)
        {
            var itemModel = new ItemModel(
                context: context,
                referenceId: referenceId);
            var siteModel = new SiteModel(
                context: context,
                siteId: itemModel.SiteId,
                formData: context.Forms);
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: referenceId);
            var site = itemModel.ReferenceType == "Sites";
            var res = new ResponseCollection(context: context);
            var selected = context.Forms.List("CurrentPermissions");
            if (selected.Any(o => o.StartsWith($"User,{context.UserId},")))
            {
                return res.Message(Messages.PermissionNotSelfChange(context: context)).ToJson();
            }
            else if (!selected.Any())
            {
                return res.Message(Messages.SelectTargets(context: context)).ToJson();
            }
            else
            {
                return res.Html("#PermissionsDialog", PermissionsDialog(
                    context: context,
                    permissionType: (Permissions.Types)selected
                        .FirstOrDefault()
                        .Split_3rd()
                        .ToLong(),
                    referenceId: referenceId,
                    disableRecordPermission: site
                        ? false
                        : siteModel.SiteSettings.PermissionForUpdating?.Any() == true)).ToJson();
            }
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static HtmlBuilder PermissionsDialog(this HtmlBuilder hb, Context context)
        {
            return hb.Div(attributes: new HtmlAttributes()
                .Id("PermissionsDialog")
                .Class("dialog")
                .Title(Displays.AdvancedSetting(context: context)));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder PermissionsDialog(
            Context context,
            Permissions.Types permissionType,
            long referenceId,
            bool disableRecordPermission)
        {
            var hb = new HtmlBuilder();
            return hb.Form(
                attributes: new HtmlAttributes()
                    .Id("PermissionsForm")
                    .Action(Locations.ItemAction(
                        context: context,
                        id: referenceId)),
                action: () => hb
                    .FieldDropDown(
                        disabled: disableRecordPermission,
                        context: context,
                        controlId: "PermissionPattern",
                        controlCss: " auto-postback",
                        labelText: Displays.Pattern(context: context),
                        optionCollection: Parameters.Permissions.Pattern
                            .ToDictionary(
                                o => o.Value.ToString(),
                                o => new ControlData(Displays.Get(
                                    context: context,
                                    id: o.Key))),
                        selectedValue: permissionType.ToLong().ToString(),
                        addSelectedValue: false,
                        action: "SetPermissions",
                        method: "post")
                    .PermissionParts(
                        context: context,
                        controlId: "PermissionParts",
                        labelText: Displays.Permissions(context: context),
                        permissionType: permissionType,
                        disableRecordPermission: disableRecordPermission)
                    .P(css: "message-dialog")
                    .Div(css: "command-center", action: () => hb
                        .Button(
                            disabled: disableRecordPermission,
                            controlId: "ChangePermissions",
                            text: Displays.Change(context: context),
                            controlCss: "button-icon validate button-positive",
                            onClick: "$p.changePermissions($(this));",
                            icon: "ui-icon-disk",
                            action: "SetPermissions",
                            method: "post")
                        .Button(
                            text: Displays.Cancel(context: context),
                            controlCss: "button-icon button-neutral",
                            onClick: "$p.closeDialog($(this));",
                            icon: "ui-icon-cancel")));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder PermissionParts(
            this HtmlBuilder hb,
            Context context,
            string controlId,
            string labelText,
            Permissions.Types permissionType,
            bool disableRecordPermission)
        {
            return hb.FieldSet(
                id: controlId,
                css: " enclosed",
                legendText: labelText,
                action: () => hb
                    .PermissionPart(
                        context: context,
                        permissionType: permissionType,
                        type: Permissions.Types.Read,
                        disableRecordPermission: disableRecordPermission)
                    .PermissionPart(
                        context: context,
                        permissionType: permissionType,
                        type: Permissions.Types.Create,
                        disableRecordPermission: disableRecordPermission)
                    .PermissionPart(
                        context: context,
                        permissionType: permissionType,
                        type: Permissions.Types.Update,
                        disableRecordPermission: disableRecordPermission)
                    .PermissionPart(
                        context: context,
                        permissionType: permissionType,
                        type: Permissions.Types.Delete,
                        disableRecordPermission: disableRecordPermission)
                    .PermissionPart(
                        context: context,
                        permissionType: permissionType,
                        type: Permissions.Types.SendMail,
                        disableRecordPermission: disableRecordPermission)
                    .PermissionPart(
                        context: context,
                        permissionType: permissionType,
                        type: Permissions.Types.Export,
                        disableRecordPermission: disableRecordPermission)
                    .PermissionPart(
                        context: context,
                        permissionType: permissionType,
                        type: Permissions.Types.Import,
                        disableRecordPermission: disableRecordPermission)
                    .PermissionPart(
                        context: context,
                        permissionType: permissionType,
                        type: Permissions.Types.ManageSite,
                        disableRecordPermission: disableRecordPermission)
                    .PermissionPart(
                        context: context,
                        permissionType: permissionType,
                        type: Permissions.Types.ManagePermission,
                        disableRecordPermission: disableRecordPermission));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder PermissionPart(
            this HtmlBuilder hb,
            Context context,
            Permissions.Types permissionType,
            Permissions.Types type,
            bool disableRecordPermission)
        {
            return hb.FieldCheckBox(
                disabled: disableRecordPermission,
                controlId: type.ToString(),
                fieldCss: "field-auto-thin w200",
                controlCss: " always-send",
                labelText: Displays.Get(
                    context: context,
                    id: type.ToString()),
                _checked: (permissionType & type) > 0,
                dataId: type.ToLong().ToString());
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static bool Same(string value1, string value2)
        {
            return
                value1.Split_1st() == value2.Split_1st() &&
                value1.Split_2nd() == value2.Split_2nd();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static Permissions.Types GetPermissionTypeByForm(Context context)
        {
            var permissionType = Permissions.Types.NotSet;
            if (context.Forms.Bool("Read")) permissionType |= Permissions.Types.Read;
            if (context.Forms.Bool("Create")) permissionType |= Permissions.Types.Create;
            if (context.Forms.Bool("Update")) permissionType |= Permissions.Types.Update;
            if (context.Forms.Bool("Delete")) permissionType |= Permissions.Types.Delete;
            if (context.Forms.Bool("SendMail")) permissionType |= Permissions.Types.SendMail;
            if (context.Forms.Bool("Export")) permissionType |= Permissions.Types.Export;
            if (context.Forms.Bool("Import")) permissionType |= Permissions.Types.Import;
            if (context.Forms.Bool("ManageSite")) permissionType |= Permissions.Types.ManageSite;
            if (context.Forms.Bool("ManagePermission")) permissionType |= Permissions.Types.ManagePermission;
            return permissionType;
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string PermissionForRecord(Context context, long referenceId)
        {
            var ss = SiteSettingsUtilities.Get(
                context: context,
                siteModel: new ItemModel(
                    context: context,
                    referenceId: referenceId).GetSite(
                        context: context),
                referenceId: referenceId);
            if (!context.CanManagePermission(ss: ss))
            {
                return Error.Types.HasNotPermission.MessageJson(context: context);
            }
            return new ResponseCollection(context: context)
                .Html(
                    "#FieldSetRecordAccessControlEditor",
                    new HtmlBuilder().FieldSetRecordAccessControl(
                        context: context,
                        ss: ss))
                .RemoveAttr("#FieldSetRecordAccessControl", "data-action")
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder FieldSetRecordAccessControl(
            this HtmlBuilder hb, Context context, SiteSettings ss)
        {
            var permissionsForCreating = PermissionForCreating(ss);
            var permissionsForUpdating = PermissionForUpdating(ss);
            return hb
                .FieldSet(
                    id: "FieldSetRecordAccessControlForCreating",
                    css: " enclosed",
                    legendText: Displays.PermissionForCreating(context: context),
                    action: () => hb
                        .Div(
                            id: "PermissionForCreating",
                            action: () => hb
                                .CurrentPermissionForCreating(
                                    context: context,
                                    ss: ss,
                                    permissions: permissionsForCreating.Where(o => !o.Source))
                                .SourcePermissionForCreating(
                                    context: context,
                                    ss: ss,
                                    permissions: permissionsForCreating.Where(o => o.Source))))
                .FieldSet(
                    id: "FieldSetRecordAccessControlForUpdating",
                    css: " enclosed",
                    legendText: Displays.PermissionForUpdating(context: context),
                    action: () => hb
                        .Div(
                            id: "PermissionForUpdating",
                            action: () => hb
                                .CurrentPermissionForUpdating(
                                    context: context,
                                    ss: ss,
                                    permissions: permissionsForUpdating.Where(o => !o.Source))
                                .SourcePermissionForUpdating(
                                    context: context,
                                    ss: ss,
                                    permissions: permissionsForUpdating.Where(o => o.Source))));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder CurrentPermissionForCreating(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            IEnumerable<Permission> permissions)
        {
            return hb.FieldSelectable(
                controlId: "CurrentPermissionForCreating",
                fieldCss: "field-vertical both",
                controlContainerCss: "container-selectable",
                controlCss: " send-all",
                labelText: Displays.CurrentSettings(context: context),
                listItemCollection: permissions.ToDictionary(
                    o => o.Key(),
                    o => o.ControlData(context: context, ss: ss)),
                commandOptionPositionIsTop: true,
                commandOptionAction: () => hb
                    .Div(css: "command-left", action: () => hb
                        .Button(
                            controlId: "OpenPermissionForCreatingDialog",
                            controlCss: "button-icon post",
                            text: Displays.AdvancedSetting(context: context),
                            onClick: "$p.openPermissionForCreatingDialog($(this));",
                            icon: "ui-icon-gear",
                            action: "OpenPermissionForCreatingDialog",
                            method: "post")
                        .Button(
                            controlId: "DeletePermissionForCreating",
                            controlCss: "button-icon post",
                            text: Displays.ToDisable(context: context),
                            onClick: "$p.setPermissionForCreating($(this));",
                            icon: "ui-icon-circle-triangle-e",
                            action: "SetPermissionForCreating",
                            method: "delete")));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder CurrentPermissionForUpdating(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            IEnumerable<Permission> permissions)
        {
            return hb.FieldSelectable(
                controlId: "CurrentPermissionForUpdating",
                fieldCss: "field-vertical both",
                controlContainerCss: "container-selectable",
                controlCss: " send-all",
                labelText: Displays.CurrentSettings(context: context),
                listItemCollection: permissions.ToDictionary(
                    o => o.Key(),
                    o => o.ControlData(context: context, ss: ss)),
                commandOptionPositionIsTop: true,
                commandOptionAction: () => hb
                    .Div(css: "command-left", action: () => hb
                        .Button(
                            controlId: "OpenPermissionForUpdatingDialog",
                            controlCss: "button-icon post",
                            text: Displays.AdvancedSetting(context: context),
                            onClick: "$p.openPermissionForUpdatingDialog($(this));",
                            icon: "ui-icon-gear",
                            action: "OpenPermissionForUpdatingDialog",
                            method: "post")
                        .Button(
                            controlId: "DeletePermissionForUpdating",
                            controlCss: "button-icon post",
                            text: Displays.ToDisable(context: context),
                            onClick: "$p.setPermissionForUpdating($(this));",
                            icon: "ui-icon-circle-triangle-e",
                            action: "SetPermissionForUpdating",
                            method: "delete")));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder SourcePermissionForCreating(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            IEnumerable<Permission> permissions)
        {
            return hb.FieldSelectable(
                controlId: "SourcePermissionForCreating",
                fieldCss: "field-vertical",
                controlContainerCss: "container-selectable",
                controlWrapperCss: " h300",
                labelText: Displays.OptionList(context: context),
                listItemCollection: permissions.ToDictionary(
                    o => o.Key(), o => o.ControlData(
                        context: context,
                        ss: ss,
                        withType: false)),
                commandOptionPositionIsTop: true,
                commandOptionAction: () => hb
                    .Div(css: "command-left", action: () => hb
                        .Button(
                            controlId: "AddPermissionForCreating",
                            controlCss: "button-icon post",
                            text: Displays.ToEnable(context: context),
                            onClick: "$p.setPermissionForCreating($(this));",
                            icon: "ui-icon-circle-triangle-w",
                            action: "SetPermissionForCreating",
                            method: "post")));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder SourcePermissionForUpdating(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            IEnumerable<Permission> permissions)
        {
            return hb.FieldSelectable(
                controlId: "SourcePermissionForUpdating",
                fieldCss: "field-vertical",
                controlContainerCss: "container-selectable",
                controlWrapperCss: " h300",
                labelText: Displays.OptionList(context: context),
                listItemCollection: permissions.ToDictionary(
                    o => o.Key(), o => o.ControlData(
                        context: context,
                        ss: ss,
                        withType: false)),
                commandOptionPositionIsTop: true,
                commandOptionAction: () => hb
                    .Div(css: "command-left", action: () => hb
                        .Button(
                            controlId: "AddPermissionForUpdating",
                            controlCss: "button-icon post",
                            text: Displays.ToEnable(context: context),
                            onClick: "$p.setPermissionForUpdating($(this));",
                            icon: "ui-icon-circle-triangle-w",
                            action: "SetPermissionForUpdating",
                            method: "post")));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static List<Permission> PermissionForCreating(SiteSettings ss)
        {
            var type = (Permissions.Types)Parameters.Permissions.General;
            var permissions = new List<Permission>
            {
                ss.GetPermissionForCreating("Dept"),
                ss.GetPermissionForCreating("Group"),
                ss.GetPermissionForCreating("User")
            };
            permissions.AddRange(ss.Columns
                .Where(o => o.Type != Column.Types.Normal)
                .Where(o => o.ColumnName != "Creator" && o.ColumnName != "Updator")
                .Select(o => ss.GetPermissionForCreating(o.ColumnName)));
            return permissions;
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static List<Permission> PermissionForUpdating(SiteSettings ss)
        {
            var type = (Permissions.Types)Parameters.Permissions.General;
            var permissions = new List<Permission>
            {
                ss.GetPermissionForUpdating("Dept"),
                ss.GetPermissionForUpdating("Group"),
                ss.GetPermissionForUpdating("User")
            };
            permissions.AddRange(ss.Columns
                .Where(o => o.Type != Column.Types.Normal)
                .Where(o => o.ColumnName != "Creator" && o.ColumnName != "Updator")
                .Select(o => ss.GetPermissionForUpdating(o.ColumnName)));
            return permissions;
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string SetPermissionForCreating(Context context, long referenceId)
        {
            var itemModel = new ItemModel(
                context: context,
                referenceId: referenceId);
            var siteModel = new SiteModel(
                context: context,
                siteId: itemModel.SiteId,
                formData: context.Forms);
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: referenceId);
            var invalid = PermissionValidators.OnUpdating(
                context: context,
                ss: siteModel.SiteSettings);
            switch (invalid.Type)
            {
                case Error.Types.None: break;
                default: return invalid.MessageJson(context: context);
            }
            var res = new ResponseCollection(context: context);
            var selectedCurrentPermissionForCreating = context.Forms.List("CurrentPermissionForCreating");
            var selectedSourcePermissionForCreating = context.Forms.List("SourcePermissionForCreating");
            if (context.Forms.ControlId() != "AddPermissionForCreating" &&
                selectedCurrentPermissionForCreating.Any(o =>
                    o.StartsWith($"User,{context.UserId},")))
            {
                res.Message(Messages.PermissionNotSelfChange(context: context));
            }
            else
            {
                var permissionForCreating = PermissionForCreating(siteModel.SiteSettings);
                var currentPermissionForCreating = context.Forms.Exists("CurrentPermissionForCreatingAll")
                    ? Permissions.Get(context.Forms.List("CurrentPermissionForCreatingAll"))
                    : permissionForCreating.Where(o => !o.Source).ToList();
                var sourcePermissionForCreating = permissionForCreating
                    .Where(o => !currentPermissionForCreating.Any(p =>
                        p.NameAndId() == o.NameAndId()))
                    .ToList();
                switch (context.Forms.ControlId())
                {
                    case "AddPermissionForCreating":
                        currentPermissionForCreating.AddRange(
                            Permissions.Get(selectedSourcePermissionForCreating,
                                Permissions.General()));
                        sourcePermissionForCreating.RemoveAll(o =>
                            selectedSourcePermissionForCreating.Any(p =>
                                Same(p, o.Key())));
                        res
                            .Html("#CurrentPermissionForCreating", PermissionListItem(
                                context: context,
                                ss: siteModel.SiteSettings,
                                permissions: currentPermissionForCreating,
                                selectedValueTextCollection: selectedSourcePermissionForCreating))
                            .Html("#SourcePermissionForCreating", PermissionListItem(
                                context: context,
                                ss: siteModel.SiteSettings,
                                permissions: sourcePermissionForCreating,
                                withType: false))
                            .SetData("#CurrentPermissionForCreating")
                            .SetData("#SourcePermissionForCreating");
                        break;
                    case "PermissionForCreatingPattern":
                        res.ReplaceAll(
                            "#PermissionForCreatingParts",
                            new HtmlBuilder().PermissionParts(
                                context: context,
                                controlId: "PermissionForCreatingParts",
                                labelText: Displays.Permissions(context: context),
                                permissionType: (Permissions.Types)context.Forms.Long(
                                    "PermissionForCreatingPattern"),
                                disableRecordPermission: false));
                        break;
                    case "ChangePermissionForCreating":
                        res.ChangePermissions(
                            context: context,
                            ss: siteModel.SiteSettings,
                            selector: "#CurrentPermissionForCreating",
                            currentPermissions: currentPermissionForCreating,
                            selectedCurrentPermissions: selectedCurrentPermissionForCreating,
                            permissionType: GetPermissionTypeByForm(context: context));
                        break;
                    case "DeletePermissionForCreating":
                        sourcePermissionForCreating.AddRange(
                            currentPermissionForCreating.Where(o =>
                                selectedCurrentPermissionForCreating.Any(p =>
                                    Same(p, o.Key()))));
                        currentPermissionForCreating.RemoveAll(o =>
                            selectedCurrentPermissionForCreating.Any(p =>
                                Same(p, o.Key())));
                        res
                            .Html("#CurrentPermissionForCreating", PermissionListItem(
                                context: context,
                                ss: siteModel.SiteSettings,
                                permissions: currentPermissionForCreating))
                            .Html("#SourcePermissionForCreating", PermissionListItem(
                                context: context,
                                ss: siteModel.SiteSettings,
                                permissions: sourcePermissionForCreating,
                                selectedValueTextCollection: selectedCurrentPermissionForCreating,
                                withType: false))
                            .SetData("#CurrentPermissionForCreating")
                            .SetData("#SourcePermissionForCreating");
                        break;
                }
            }
            return res
                .SetMemory("formChanged", true)
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string OpenPermissionForCreatingDialog(Context context, long referenceId)
        {
            var res = new ResponseCollection(context: context);
            var selected = context.Forms.List("CurrentPermissionForCreating");
            if (!selected.Any())
            {
                return res.Message(Messages.SelectTargets(context: context)).ToJson();
            }
            else
            {
                return res.Html("#PermissionForCreatingDialog", PermissionForCreatingDialog(
                    context: context,
                    permissionType: (Permissions.Types)selected
                        .FirstOrDefault()
                        .Split_3rd()
                        .ToLong(),
                    referenceId: referenceId)).ToJson();
            }
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static HtmlBuilder PermissionForCreatingDialog(this HtmlBuilder hb, Context context)
        {
            return hb.Div(attributes: new HtmlAttributes()
                .Id("PermissionForCreatingDialog")
                .Class("dialog")
                .Title(Displays.AdvancedSetting(context: context)));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder PermissionForCreatingDialog(
            Context context, Permissions.Types permissionType, long referenceId)
        {
            var itemModel = new ItemModel(
                context: context,
                referenceId: referenceId);
            var siteModel = new SiteModel(
                context: context,
                siteId: itemModel.SiteId,
                formData: context.Forms);
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: referenceId);
            var hb = new HtmlBuilder();
            return hb.Form(
                attributes: new HtmlAttributes()
                    .Id("PermissionForCreatingForm")
                    .Action(Locations.ItemAction(
                        context: context,
                        id: referenceId)),
                action: () => hb
                    .FieldDropDown(
                        context: context,
                        controlId: "PermissionForCreatingPattern",
                        controlCss: " auto-postback",
                        labelText: Displays.Pattern(context: context),
                        optionCollection: Parameters.Permissions.Pattern
                            .ToDictionary(
                                o => o.Value.ToString(),
                                o => new ControlData(Displays.Get(
                                    context: context,
                                    id: o.Key))),
                        selectedValue: permissionType.ToLong().ToString(),
                        addSelectedValue: false,
                        action: "SetPermissionForCreating",
                        method: "post")
                    .PermissionParts(
                        context: context,
                        controlId: "PermissionForCreatingParts",
                        labelText: Displays.Permissions(context: context),
                        permissionType: permissionType,
                        disableRecordPermission: false)
                    .P(css: "message-dialog")
                    .Div(css: "command-center", action: () => hb
                        .Button(
                            controlId: "ChangePermissionForCreating",
                            text: Displays.Change(context: context),
                            controlCss: "button-icon validate button-positive",
                            onClick: "$p.changePermissionForCreating($(this));",
                            icon: "ui-icon-disk",
                            action: "SetPermissionForCreating",
                            method: "post")
                        .Button(
                            text: Displays.Cancel(context: context),
                            controlCss: "button-icon button-neutral",
                            onClick: "$p.closeDialog($(this));",
                            icon: "ui-icon-cancel")));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string SetPermissionForUpdating(Context context, long referenceId)
        {
            var itemModel = new ItemModel(
                context: context,
                referenceId: referenceId);
            var siteModel = new SiteModel(
                context: context,
                siteId: itemModel.SiteId,
                formData: context.Forms);
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: referenceId);
            var invalid = PermissionValidators.OnUpdating(
                context: context,
                ss: siteModel.SiteSettings);
            switch (invalid.Type)
            {
                case Error.Types.None: break;
                default: return invalid.MessageJson(context: context);
            }
            var res = new ResponseCollection(context: context);
            var selectedCurrentPermissionForUpdating = context.Forms.List("CurrentPermissionForUpdating");
            var selectedSourcePermissionForUpdating = context.Forms.List("SourcePermissionForUpdating");
            if (context.Forms.ControlId() != "AddPermissionForUpdating" &&
                selectedCurrentPermissionForUpdating.Any(o =>
                    o.StartsWith($"User,{context.UserId},")))
            {
                res.Message(Messages.PermissionNotSelfChange(context: context));
            }
            else
            {
                var permissionForUpdating = PermissionForUpdating(siteModel.SiteSettings);
                var currentPermissionForUpdating = context.Forms.Exists("CurrentPermissionForUpdatingAll")
                    ? Permissions.Get(context.Forms.List("CurrentPermissionForUpdatingAll"))
                    : permissionForUpdating.Where(o => !o.Source).ToList();
                var sourcePermissionForUpdating = permissionForUpdating
                    .Where(o => !currentPermissionForUpdating.Any(p =>
                        p.NameAndId() == o.NameAndId()))
                    .ToList();
                switch (context.Forms.ControlId())
                {
                    case "AddPermissionForUpdating":
                        currentPermissionForUpdating.AddRange(
                            Permissions.Get(selectedSourcePermissionForUpdating,
                                Permissions.General()));
                        sourcePermissionForUpdating.RemoveAll(o =>
                            selectedSourcePermissionForUpdating.Any(p =>
                                Same(p, o.Key())));
                        res
                            .Html("#CurrentPermissionForUpdating", PermissionListItem(
                                context: context,
                                ss: siteModel.SiteSettings,
                                permissions: currentPermissionForUpdating,
                                selectedValueTextCollection: selectedSourcePermissionForUpdating))
                            .Html("#SourcePermissionForUpdating", PermissionListItem(
                                context: context,
                                ss: siteModel.SiteSettings,
                                permissions: sourcePermissionForUpdating,
                                withType: false))
                            .SetData("#CurrentPermissionForUpdating")
                            .SetData("#SourcePermissionForUpdating");
                        break;
                    case "PermissionForUpdatingPattern":
                        res.ReplaceAll(
                            "#PermissionForUpdatingParts",
                            new HtmlBuilder().PermissionParts(
                                context: context,
                                controlId: "PermissionForUpdatingParts",
                                labelText: Displays.Permissions(context: context),
                                permissionType: (Permissions.Types)context.Forms.Long(
                                    "PermissionForUpdatingPattern"),
                                disableRecordPermission: false));
                        break;
                    case "ChangePermissionForUpdating":
                        res.ChangePermissions(
                            context: context,
                            ss: siteModel.SiteSettings,
                            selector: "#CurrentPermissionForUpdating",
                            currentPermissions: currentPermissionForUpdating,
                            selectedCurrentPermissions: selectedCurrentPermissionForUpdating,
                            permissionType: GetPermissionTypeByForm(context: context));
                        break;
                    case "DeletePermissionForUpdating":
                        sourcePermissionForUpdating.AddRange(
                            currentPermissionForUpdating.Where(o =>
                                selectedCurrentPermissionForUpdating.Any(p =>
                                    Same(p, o.Key()))));
                        currentPermissionForUpdating.RemoveAll(o =>
                            selectedCurrentPermissionForUpdating.Any(p =>
                                Same(p, o.Key())));
                        res
                            .Html("#CurrentPermissionForUpdating", PermissionListItem(
                                context: context,
                                ss: siteModel.SiteSettings,
                                permissions: currentPermissionForUpdating))
                            .Html("#SourcePermissionForUpdating", PermissionListItem(
                                context: context,
                                ss: siteModel.SiteSettings,
                                permissions: sourcePermissionForUpdating,
                                selectedValueTextCollection: selectedCurrentPermissionForUpdating,
                                withType: false))
                            .SetData("#CurrentPermissionForUpdating")
                            .SetData("#SourcePermissionForUpdating");
                        break;
                }
            }
            return res
                .SetMemory("formChanged", true)
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string OpenPermissionForUpdatingDialog(Context context, long referenceId)
        {
            var res = new ResponseCollection(context: context);
            var selected = context.Forms.List("CurrentPermissionForUpdating");
            if (!selected.Any())
            {
                return res.Message(Messages.SelectTargets(context: context)).ToJson();
            }
            else
            {
                return res.Html("#PermissionForUpdatingDialog", PermissionForUpdatingDialog(
                    context: context,
                    permissionType: (Permissions.Types)selected
                        .FirstOrDefault()
                        .Split_3rd()
                        .ToLong(),
                    referenceId: referenceId)).ToJson();
            }
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static HtmlBuilder PermissionForUpdatingDialog(this HtmlBuilder hb, Context context)
        {
            return hb.Div(attributes: new HtmlAttributes()
                .Id("PermissionForUpdatingDialog")
                .Class("dialog")
                .Title(Displays.AdvancedSetting(context: context)));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder PermissionForUpdatingDialog(
            Context context, Permissions.Types permissionType, long referenceId)
        {
            var itemModel = new ItemModel(
                context: context,
                referenceId: referenceId);
            var siteModel = new SiteModel(
                context: context,
                siteId: itemModel.SiteId,
                formData: context.Forms);
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: referenceId);
            var hb = new HtmlBuilder();
            return hb.Form(
                attributes: new HtmlAttributes()
                    .Id("PermissionForUpdatingForm")
                    .Action(Locations.ItemAction(
                        context: context,
                        id: referenceId)),
                action: () => hb
                    .FieldDropDown(
                        context: context,
                        controlId: "PermissionForUpdatingPattern",
                        controlCss: " auto-postback",
                        labelText: Displays.Pattern(context: context),
                        optionCollection: Parameters.Permissions.Pattern
                            .ToDictionary(
                                o => o.Value.ToString(),
                                o => new ControlData(Displays.Get(
                                    context: context,
                                    id: o.Key))),
                        selectedValue: permissionType.ToLong().ToString(),
                        addSelectedValue: false,
                        action: "SetPermissionForUpdating",
                        method: "post")
                    .PermissionParts(
                        context: context,
                        controlId: "PermissionForUpdatingParts",
                        labelText: Displays.Permissions(context: context),
                        permissionType: permissionType,
                        disableRecordPermission: false)
                    .P(css: "message-dialog")
                    .Div(css: "command-center", action: () => hb
                        .Button(
                            controlId: "ChangePermissionForUpdating",
                            text: Displays.Change(context: context),
                            controlCss: "button-icon validate button-positive",
                            onClick: "$p.changePermissionForUpdating($(this));",
                            icon: "ui-icon-disk",
                            action: "SetPermissionForUpdating",
                            method: "post")
                        .Button(
                            text: Displays.Cancel(context: context),
                            controlCss: "button-icon button-neutral",
                            onClick: "$p.closeDialog($(this));",
                            icon: "ui-icon-cancel")));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string ColumnAccessControl(Context context, long referenceId)
        {
            var ss = SiteSettingsUtilities.Get(
                context: context,
                siteModel: new ItemModel(
                    context: context,
                    referenceId: referenceId)
                        .GetSite(context: context),
                referenceId: referenceId);
            if (!context.CanManagePermission(ss: ss))
            {
                return Error.Types.HasNotPermission.MessageJson(context: context);
            }
            return new ResponseCollection(context: context)
                .Html(
                    "#FieldSetColumnAccessControlEditor",
                    new HtmlBuilder().ColumnAccessControl(
                        context: context,
                        ss: ss))
                .RemoveAttr("#FieldSetColumnAccessControl", "data-action")
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder ColumnAccessControl(
            this HtmlBuilder hb, Context context, SiteSettings ss)
        {
            return hb.FieldSet(
                id: "FieldSetColumnAccessControl",
                css: " enclosed",
                legendText: Displays.ColumnAccessControl(context: context),
                action: () => hb
                    .Div(
                        id: "ColumnAccessControl",
                        action: () => hb
                            .ColumnAccessControl(
                                context: context,
                                ss: ss,
                                type: "Create",
                                labelText: Displays.CreateColumnAccessControl(context: context))
                            .ColumnAccessControl(
                                context: context,
                                ss: ss,
                                type: "Read",
                                labelText: Displays.ReadColumnAccessControl(context: context))
                            .ColumnAccessControl(
                                context: context,
                                ss: ss,
                                type: "Update",
                                labelText: Displays.UpdateColumnAccessControl(context: context))));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder ColumnAccessControl(
            this HtmlBuilder hb, Context context, SiteSettings ss, string type, string labelText)
        {
            return hb.FieldSelectable(
                controlId: type + "ColumnAccessControl",
                fieldCss: "field-vertical",
                controlContainerCss: "container-selectable",
                controlWrapperCss: " h300",
                controlCss: " send-all",
                labelText: labelText,
                listItemCollection: ss.ColumnAccessControlOptions(
                    context: context,
                    type: type),
                commandOptionPositionIsTop: true,
                commandOptionAction: () => hb
                    .Div(css: "command-left", action: () => hb
                        .Button(
                            controlId: type + "OpenColumnAccessControlDialog",
                            controlCss: "button-icon post",
                            text: Displays.AdvancedSetting(context: context),
                            onClick: "$p.openColumnAccessControlDialog($(this));",
                            icon: "ui-icon-gear",
                            action: "OpenColumnAccessControlDialog",
                            method: "post")));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static HtmlBuilder ColumnAccessControlDialog(this HtmlBuilder hb, Context context)
        {
            return hb.Div(attributes: new HtmlAttributes()
                .Id("ColumnAccessControlDialog")
                .Class("dialog")
                .Title(Displays.AdvancedSetting(context: context)));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string SetColumnAccessControl(Context context, long referenceId)
        {
            var itemModel = new ItemModel(
                context: context,
                referenceId: referenceId);
            var siteModel = new SiteModel(
                context: context,
                siteId: itemModel.SiteId,
                formData: context.Forms);
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: referenceId);
            var invalid = PermissionValidators.OnUpdating(
                context: context,
                ss: siteModel.SiteSettings);
            switch (invalid.Type)
            {
                case Error.Types.None: break;
                default: return invalid.MessageJson(context: context);
            }
            var res = new ResponseCollection(context: context);
            var type = context.Forms.Data("ColumnAccessControlType");
            var selected = context.Forms.List("ColumnAccessControl")
                .Select(o => o.Deserialize<ColumnAccessControl>())
                .ToList();
            var columnAccessControl = context.Forms.List("ColumnAccessControlAll")
                .Select(o => ColumnAccessControl(
                    context: context,
                    columnAccessControl: o.Deserialize<ColumnAccessControl>(),
                    selected: selected))
                .ToList();
            var listItemCollection = siteModel.SiteSettings.ColumnAccessControlOptions(
                context: context,
                type: type,
                columnAccessControls: columnAccessControl);
            res
                .CloseDialog()
                .Html("#" + type + "ColumnAccessControl", new HtmlBuilder().SelectableItems(
                    listItemCollection: listItemCollection,
                    selectedValueTextCollection: columnAccessControl
                        .Where(o => selected.Any(p => p.ColumnName == o.ColumnName))
                        .Select(o => o.ToJson())))
                .SetData("#" + type + "ColumnAccessControl");
            return res
                .SetMemory("formChanged", true)
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static ColumnAccessControl ColumnAccessControl(
            Context context,
            ColumnAccessControl columnAccessControl,
            List<ColumnAccessControl> selected)
        {
            var allowdUsers = new Dictionary<string, bool>
            {
                { "Creator", context.Forms.Bool("CreatorAllowed") },
                { "Updator", context.Forms.Bool("UpdatorAllowed") },
                { "Manager", context.Forms.Bool("ManagerAllowed") },
                { "Owner", context.Forms.Bool("OwnerAllowed") },
            }
                .Where(o => o.Value)
                .Select(o => o.Key)
                .ToList();
            if (selected.Any(o => o.ColumnName == columnAccessControl.ColumnName))
            {
                columnAccessControl.Depts = new List<int>();
                columnAccessControl.Groups = new List<int>();
                columnAccessControl.Users = new List<int>();
                CurrentColumnAccessControlAll(context: context).ForEach(permission =>
                {
                    switch (permission.Name)
                    {
                        case "Dept":
                            columnAccessControl.Depts.Add(permission.Id);
                            break;
                        case "Group":
                            columnAccessControl.Groups.Add(permission.Id);
                            break;
                        case "User":
                            columnAccessControl.Users.Add(permission.Id);
                            break;
                    }
                });
                columnAccessControl.RecordUsers = allowdUsers;
                columnAccessControl.Type = GetPermissionTypeByForm(context: context);
            }
            return columnAccessControl;
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static List<Permission> CurrentColumnAccessControlAll(Context context)
        {
            return context.Forms.List("CurrentColumnAccessControlAll")
                .Select(data => new Permission(
                    name: data.Split_1st(),
                    id: data.Split_2nd().ToInt(),
                    type: Permissions.Types.NotSet))
                .ToList();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string OpenColumnAccessControlDialog(Context context, long referenceId)
        {
            var ss = SiteSettingsUtilities.Get(
                context: context,
                siteModel: new ItemModel(
                    context: context,
                    referenceId: referenceId)
                        .GetSite(context: context),
                referenceId: referenceId);
            if (!context.CanManagePermission(ss: ss))
            {
                return Error.Types.HasNotPermission.MessageJson(context: context);
            }
            var res = new ResponseCollection(context: context);
            var type = ColumnAccessControlType(context: context);
            var selected = context.Forms.List(type + "ColumnAccessControl");
            if (!selected.Any())
            {
                return res.Message(Messages.SelectTargets(context: context)).ToJson();
            }
            else
            {
                return res.Html("#ColumnAccessControlDialog", ColumnAccessControlDialog(
                    context: context,
                    ss: ss,
                    type: type,
                    columnAccessControl: selected.FirstOrDefault()
                        .Deserialize<ColumnAccessControl>(),
                    referenceId: referenceId)).ToJson();
            }
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static string ColumnAccessControlType(Context context)
        {
            switch (context.Forms.ControlId())
            {
                case "CreateOpenColumnAccessControlDialog": return "Create";
                case "ReadOpenColumnAccessControlDialog": return "Read";
                case "UpdateOpenColumnAccessControlDialog": return "Update";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder ColumnAccessControlDialog(
            Context context,
            SiteSettings ss,
            string type,
            ColumnAccessControl columnAccessControl,
            long referenceId)
        {
            var hb = new HtmlBuilder();
            return hb.Form(
                attributes: new HtmlAttributes()
                    .Id("ColumnAccessControlForm")
                    .Action(Locations.ItemAction(
                        context: context,
                        id: referenceId)),
                action: () => hb
                    .Div(
                        id: "ColumnAccessControlTabsContainer",
                        css: "tab-container",
                        action: () => hb
                            .Ul(
                                id: "ColumnAccessControlTabs",
                                action: () => hb
                                    .Li(action: () => hb
                                        .A(
                                            href: "#ColumnAccessControlBasicTab",
                                            text: Displays.Basic(context: context)))
                                    .Li(action: () => hb
                                        .A(
                                            href: "#ColumnAccessControlOhtersTab",
                                            text: Displays.Others(context: context))))
                            .ColumnAccessControlBasicTab(
                                context: context,
                                ss: ss,
                                type: type,
                                columnAccessControl: columnAccessControl,
                                referenceId: referenceId)
                            .ColumnAccessControlOhtersTab(
                                context: context,
                                ss: ss,
                                type: type,
                                columnAccessControl: columnAccessControl,
                                referenceId: referenceId))
                    .P(css: "message-dialog")
                    .Div(css: "command-center", action: () => hb
                        .Button(
                            text: Displays.Change(context: context),
                            controlCss: "button-icon validate button-positive",
                            onClick: "$p.changeColumnAccessControl($(this), '" + type + "');",
                            icon: "ui-icon-disk",
                            action: "SetColumnAccessControl",
                            method: "post")
                        .Button(
                            text: Displays.Cancel(context: context),
                            controlCss: "button-icon button-neutral",
                            onClick: "$p.closeDialog($(this));",
                            icon: "ui-icon-cancel"))
                    .Hidden(controlId: "ColumnAccessControlType", value: type)
                    .Hidden(
                        controlId: "ColumnAccessControlNullableOnly",
                        value: ss.ColumnAccessControlNullableOnly(type).ToString()));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder ColumnAccessControlBasicTab(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            string type,
            ColumnAccessControl columnAccessControl,
            long referenceId)
        {
            var currentPermissions = columnAccessControl.GetPermissions(ss: ss);
            var sourcePermissions = SourceCollection(
                context: context,
                ss: ss,
                searchText: context.Forms.Data("SearchColumnAccessControlElements"),
                currentPermissions: currentPermissions);
            var offset = context.Forms.Int("ColumnAccessControlSourceOffset");
            return hb.TabsPanelField(id: "ColumnAccessControlBasicTab", action: () => hb
                .Div(id: "ColumnAccessControlEditor", action: () => hb
                    .FieldSelectable(
                        controlId: "CurrentColumnAccessControl",
                        fieldCss: "field-vertical both",
                        controlContainerCss: "container-selectable",
                        controlCss: " always-send send-all",
                        labelText: Displays.Permissions(context: context),
                        listItemCollection: currentPermissions.ToDictionary(
                            o => o.Key(), o => o.ControlData(
                                context: context,
                                ss: ss,
                                withType: false)),
                        commandOptionPositionIsTop: true,
                        commandOptionAction: () => hb
                            .Div(css: "command-left", action: () => hb
                                .Button(
                                    controlCss: "button-icon",
                                    text: Displays.DeletePermission(context: context),
                                    onClick: "$p.deleteColumnAccessControl();",
                                    icon: "ui-icon-circle-triangle-e")))
                    .FieldSelectable(
                        controlId: "SourceColumnAccessControl",
                        fieldCss: "field-vertical",
                        controlContainerCss: "container-selectable",
                        controlWrapperCss: " h300",
                        labelText: Displays.OptionList(context: context),
                        listItemCollection: sourcePermissions
                            .Page(offset)
                            .ListItemCollection(
                                context: context,
                                ss: ss,
                                withType: false),
                        commandOptionPositionIsTop: true,
                        action: "Permissions",
                        method: "post",
                        commandOptionAction: () => hb
                            .Div(css: "command-left", action: () => hb
                                .Button(
                                    controlCss: "button-icon",
                                    text: Displays.AddPermission(context: context),
                                    onClick: "$p.addColumnAccessControl();",
                                    icon: "ui-icon-circle-triangle-w")
                                .TextBox(
                                    controlId: "SearchColumnAccessControl",
                                    controlCss: " auto-postback w100",
                                    placeholder: Displays.Search(context: context),
                                    action: "SearchColumnAccessControl",
                                    method: "post")
                                .Button(
                                    text: Displays.Search(context: context),
                                    controlCss: "button-icon",
                                    onClick: "$p.send($('#SearchColumnAccessControl'));",
                                    icon: "ui-icon-search")))
                    .Hidden(
                        controlId: "SourceColumnAccessControlOffset",
                        css: "always-send",
                        value: Paging.NextOffset(
                            offset: offset,
                            totalCount: sourcePermissions.Count(),
                            pageSize: Parameters.Permissions.PageSize)
                                .ToString())));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder ColumnAccessControlOhtersTab(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            string type,
            ColumnAccessControl columnAccessControl,
            long referenceId)
        {
            return hb.TabsPanelField(id: "ColumnAccessControlOhtersTab", action: () => hb
                .PermissionParts(
                    context: context,
                    controlId: "ColumnAccessControlParts",
                    labelText: Displays.RequiredPermission(context: context),
                    permissionType: columnAccessControl.Type ?? Permissions.Types.NotSet,
                    disableRecordPermission: false)
                .FieldSet(
                    css: " enclosed",
                    legendText: Displays.AllowedUsers(context: context),
                    action: () => hb
                        .AllowedUser(
                            context: context,
                            ss: ss,
                            columnAccessControl: columnAccessControl,
                            columnName: "Creator")
                        .AllowedUser(
                            context: context,
                            ss: ss,
                            columnAccessControl: columnAccessControl,
                            columnName: "Updator")
                        .AllowedUser(
                            context: context,
                            ss: ss,
                            columnAccessControl: columnAccessControl,
                            columnName: "Manager")
                        .AllowedUser(
                            context: context,
                            ss: ss,
                            columnAccessControl: columnAccessControl,
                            columnName: "Owner"),
                    _using: type != "Create"));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        private static HtmlBuilder AllowedUser(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            ColumnAccessControl columnAccessControl,
            string columnName)
        {
            return hb.FieldCheckBox(
                fieldCss: "field-auto-thin w200",
                controlCss: " always-send",
                controlId: columnName + "Allowed",
                labelText: ss.GetColumn(
                    context: context,
                    columnName: columnName)?.LabelText,
                _checked: columnAccessControl.RecordUsers?.Contains(columnName) == true);
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string SearchColumnAccessControl(Context context, long referenceId)
        {
            var itemModel = new ItemModel(
                context: context,
                referenceId: referenceId);
            var siteModel = new SiteModel(
                context: context,
                siteId: itemModel.SiteId,
                formData: context.Forms);
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: referenceId);
            var invalid = PermissionValidators.OnUpdating(
                context: context,
                ss: siteModel.SiteSettings);
            switch (invalid.Type)
            {
                case Error.Types.None: break;
                default: return invalid.MessageJson(context: context);
            }
            var res = new ResponseCollection(context: context);
            var currentPermissions = CurrentColumnAccessControlAll(context: context);
            var sourcePermissions = SourceCollection(
                context: context,
                ss: siteModel.SiteSettings,
                searchText: context.Forms.Data("SearchColumnAccessControl"),
                currentPermissions: currentPermissions);
            return res
                .Html("#SourceColumnAccessControl", PermissionListItem(
                    context: context,
                    ss: siteModel.SiteSettings,
                    permissions: sourcePermissions.Page(0),
                    selectedValueTextCollection: context.Forms.Data("SourceColumnAccessControl")
                        .Deserialize<List<string>>()?
                        .Where(o => o != string.Empty),
                    withType: false))
                .Val("#SourceColumnAccessControlOffset", Parameters.Permissions.PageSize)
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static bool HasInheritedSites(Context context, long siteId)
        {
            return Repository.ExecuteScalar_long(
                context: context,
                statements: Rds.SelectSites(
                    column: Rds.SitesColumn().SitesCount(),
                    where: Rds.SitesWhere()
                        .SiteId(siteId, _operator: "<>")
                        .InheritPermission(siteId))) > 0;
        }
    }
}
