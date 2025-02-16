using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable IL2026

namespace LinqToDB.Remote.HttpClient.Server
{
	[PublicAPI]
	public static class ServiceConfigurationExtensions
	{
		/// <summary>
		/// Adds an <see cref="LinqToDBController"/> to the list of <see cref="ApplicationPartManager.ApplicationParts"/> on the
		/// <see cref="IMvcBuilder.PartManager"/>.
		/// </summary>
		/// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
		/// <returns>The <see cref="IMvcBuilder"/>.</returns>
		public static IMvcBuilder AddLinqToDBController(this IMvcBuilder builder, string route = "api/linq2db")
		{
			return builder.Services
				.AddControllers(options => options.Conventions.Add(new ControllerRouteConvention<LinqToDBController>(route)))
				.ConfigureApplicationPartManager(apm => apm.FeatureProviders.Add(new SpecificControllerFeatureProvider<LinqToDBController>()));
		}

		/// <summary>
		/// Adds an <see cref="LinqToDBController"/> to the list of <see cref="ApplicationPartManager.ApplicationParts"/> on the
		/// <see cref="IMvcBuilder.PartManager"/>.
		/// </summary>
		/// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
		/// <returns>The <see cref="IMvcBuilder"/>.</returns>
		public static IMvcBuilder AddLinqToDBController<TContext>(this IMvcBuilder builder, string route = "api/linq2db")
			where TContext : IDataContext
		{
			if (builder.Services.All(s => s.ServiceType != typeof(ILinqService<TContext>)))
				builder.Services.AddScoped<ILinqService<TContext>>(provider => new LinqService<TContext>(provider.GetRequiredService<IDataContextFactory<TContext>>()));

			return builder.Services
				.AddControllers(options => options.Conventions.Add(new ControllerRouteConvention<LinqToDBController<TContext>>(route)))
				.ConfigureApplicationPartManager(apm => apm.FeatureProviders.Add(new SpecificControllerFeatureProvider<LinqToDBController<TContext>>()));
		}

		sealed class SpecificControllerFeatureProvider<T> : IApplicationFeatureProvider<ControllerFeature>
		{
			public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
			{
				if (!feature.Controllers.Contains(typeof(T).GetTypeInfo()))
					feature.Controllers.Add(typeof(T).GetTypeInfo());
			}
		}

		sealed class ControllerRouteConvention<T>(string route) : IControllerModelConvention
		{
			public void Apply(ControllerModel controller)
			{
				if (controller.ControllerType == typeof(T))
				{
					controller.Selectors.Clear();
					controller.Selectors.Add(new SelectorModel { AttributeRouteModel = new AttributeRouteModel { Template = route } });
				}
			}
		}
	}
}
