﻿namespace UimfApp.Users.Commands
{
	using System.Collections.Generic;
	using System.Linq;
	using CPermissions;
	using MediatR;
	using Microsoft.AspNetCore.Identity;
	using Microsoft.EntityFrameworkCore;
	using UimfApp.Infrastructure;
	using UimfApp.Infrastructure.EntityFramework;
	using UimfApp.Infrastructure.Forms;
	using UimfApp.Infrastructure.Security;
	using UimfApp.Users.Pickers;
	using UiMetadataFramework.Basic.Input;
	using UiMetadataFramework.Basic.Input.Typeahead;
	using UiMetadataFramework.Basic.Output;
	using UiMetadataFramework.Core;
	using UiMetadataFramework.Core.Binding;
	using UiMetadataFramework.MediatR;

	[MyForm(Id = "users", PostOnLoad = true, Label = "Manage users", Menu = UserMenus.Main, MenuOrderIndex = 1)]
	public class ManageUsers : IForm<ManageUsers.Request, ManageUsers.Response>, ISecureHandler
	{
		private readonly SystemPermissionManager permissionManager;
		private readonly UserContext userContext;
		private readonly UserManager<ApplicationUser> userManager;

		public ManageUsers(
			UserManager<ApplicationUser> userManager,
			SystemPermissionManager permissionManager,
			UserContext userContext)
		{
			this.userManager = userManager;
			this.permissionManager = permissionManager;
			this.userContext = userContext;
		}

		public Response Handle(Request message)
		{
			var query = this.userManager.Users
				.Include(t => t.Roles)
				.ThenInclude(t => t.Role)
				.AsNoTracking();

			if (!string.IsNullOrEmpty(message.Email))
			{
				query = query.Where(u => u.Email.Contains(message.Email));
			}

			if (!string.IsNullOrEmpty(message.Name))
			{
				query = query.Where(u => u.UserName.Contains(message.Name));
			}

			if (!string.IsNullOrEmpty(message.Id))
			{
				query = query.Where(u => u.Id.Equals(message.Id));
			}

			if (message.Roles?.Items?.Count > 0)
			{
				query = query.Where(u => u.Roles.Any(t => message.Roles.Items.Contains(t.RoleId)));
			}

			var result = query
				.OrderBy(t => t.Id)
				.Paginate(t => new Item(t, this), message.Paginator);

			return new Response
			{
				Users = result,
				Actions = this.permissionManager.CanDo(SystemAction.ManageUsers, this.userContext)
					? new ActionList(AddUser.Button())
					: null
			};
		}

		public UserAction GetPermission()
		{
			return SystemAction.ManageUsers;
		}

		private ActionList GetActions(int userId)
		{
			var result = new ActionList();

			if (this.permissionManager.CanDo(SystemAction.ManageUsers, this.userContext))
			{
				result.Actions.Add(EditUser.Button(userId));
			}

			return result;
		}

		public class Request : IRequest<Response>
		{
			[InputField(OrderIndex = 5)]
			public string Email { get; set; }

			[InputField(OrderIndex = 0)]
			public string Id { get; set; }

			[InputField(OrderIndex = 1)]
			public string Name { get; set; }

			public Paginator Paginator { get; set; }

			[TypeaheadInputField(typeof(RoleTypeaheadInlineSource))]
			public MultiSelect<int> Roles { get; set; }
		}

		public class Response : FormResponse
		{
			[OutputField(OrderIndex = -10)]
			public ActionList Actions { get; set; }

			[PaginatedData(nameof(Request.Paginator))]
			public PaginatedData<Item> Users { get; set; }
		}

		public class Item
		{
			public Item(ApplicationUser t, ManageUsers cmd)
			{
				this.Id = t.Id;
				this.Email = t.Email;
				this.Name = t.UserName;
				this.Roles = t.Roles.Select(a => a.Role.Name).ToList();
				this.Actions = cmd.GetActions(t.Id);
			}

			[OutputField(OrderIndex = 5)]
			public ActionList Actions { get; set; }

			[OutputField(OrderIndex = 3)]
			public string Email { get; set; }

			[OutputField(OrderIndex = 1)]
			public int Id { get; set; }

			[OutputField(OrderIndex = 2)]
			public string Name { get; set; }

			[OutputField(OrderIndex = 4)]
			public IEnumerable<string> Roles { get; set; }
		}
	}
}