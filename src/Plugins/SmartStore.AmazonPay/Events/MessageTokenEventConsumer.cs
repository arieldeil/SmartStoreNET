﻿using System.Collections.Generic;
using System.Linq;
using SmartStore.Core.Domain.Messages;
using SmartStore.Core.Events;
using SmartStore.Core.Plugins;
using SmartStore.Services;
using SmartStore.Services.Messages;
using SmartStore.Services.Orders;
using SmartStore.Web.Framework;

namespace SmartStore.AmazonPay.Events
{
	public class MessageTokenEventConsumer : IConsumer<MessageModelCreatedEvent>
	{
		private readonly IPluginFinder _pluginFinder;
		private readonly ICommonServices _services;
		private readonly IOrderService _orderService;

		public MessageTokenEventConsumer(
			IPluginFinder pluginFinder,
			ICommonServices services,
			IOrderService orderService)
		{
			_pluginFinder = pluginFinder;
			_services = services;
			_orderService = orderService;
		}

		public void HandleEvent(MessageModelCreatedEvent message)
		{
			if (message.MessageContext.MessageTemplate.Name != MessageTemplateNames.OrderPlacedCustomer)
				return;

			var storeId = _services.StoreContext.CurrentStore.Id;

			if (!_pluginFinder.IsPluginReady(_services.Settings, AmazonPayPlugin.SystemName, storeId))
				return;

			dynamic model = message.Model;

			if (model.Order == null)
				return;

			var orderId = model.Order.ID;

			if (orderId is int id)
			{
				var order = _orderService.GetOrderById(id);

				var isAmazonPayment = (order != null && order.PaymentMethodSystemName.IsCaseInsensitiveEqual(AmazonPayPlugin.SystemName));
				var tokenValue = (isAmazonPayment ? _services.Localization.GetResource("Plugins.Payments.AmazonPay.BillingAddressMessageNote") : "");

				model.AmazonPay = new Dictionary<string, object>
				{
					{ "BillingAddressMessageNote", tokenValue }
				};
			}
		}
	}
}