﻿using JewelryProduction.Common;
using JewelryProduction.DbContext;
using JewelryProduction.DTO;
using JewelryProduction.Interface;
using Microsoft.EntityFrameworkCore;

namespace JewelryProduction.Services
{
    public class OrderService : IOrderService
    {
        private readonly JewelryProductionContext _context;

        public OrderService(JewelryProductionContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetOrdersByYearSortedByPrice(int year)
        {
            IQueryable<Order> query = _context.Orders
                .Where(o => o.OrderDate.Year == year)
                .OrderBy(o => o.TotalPrice);

            return await query.ToListAsync();
        }
        public async Task<Dictionary<string, double>> CalculateGoldWeightByTypeInMonth(DateTime startDate, DateTime endDate)
        {
            var totalGoldWeights = await (from cr in _context.CustomerRequests
                                          join o in _context.Orders on cr.CustomizeRequestId equals o.CustomizeRequestId
                                          where o.OrderDate >= startDate && o.OrderDate <= endDate
                                          group cr by cr.GoldId into g
                                          select new
                                          {
                                              GoldId = g.Key,
                                              TotalGoldWeight = g.Sum(cr => cr.GoldWeight) ?? 0
                                          }).ToDictionaryAsync(g => g.GoldId, g => g.TotalGoldWeight);

            return totalGoldWeights;
        }
            public async Task<PagedResult<Order>> GetAllPaging(OrderPagingRequest request)
        {

            IQueryable<Order> query = _context.Orders;
            if (!string.IsNullOrEmpty(request.Keyword))
                query = query.Where(o => o.OrderId.Contains(request.Keyword) || o.CustomizeRequestId.Contains(request.Keyword) || o.Status.Contains(request.Keyword));
            var totalOrders = await query.CountAsync();
            var orders = await query
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(o => new Order()
                {
                    OrderId = o.OrderId,
                    OrderDate = o.OrderDate,
                    TotalPrice = o.TotalPrice
                })
                .ToListAsync();

            var pagedResult = new PagedResult<Order>
            {
                TotalRecords = totalOrders,
                PageSize = request.PageSize,
                PageIndex = request.PageIndex,
                Items = orders
            };

            return pagedResult;

        }

    }

}
