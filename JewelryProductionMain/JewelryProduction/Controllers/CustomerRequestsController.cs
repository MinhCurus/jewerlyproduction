﻿
using JewelryProduction.Common;
using JewelryProduction.DbContext;
using JewelryProduction.DTO;
using JewelryProduction.Interface;
using JewelryProduction.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JewelryProduction.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerRequestsController : ControllerBase
    {
        private readonly JewelryProductionContext _context;
        private readonly ICustomerRequestService _requestService;

        public CustomerRequestsController(JewelryProductionContext context, ICustomerRequestService requestService)
        {
            _context = context;
            _requestService = requestService;
        }

        // GET: api/CustomerRequests
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerRequest>>> GetCustomerRequests()
        {
            return await _context.CustomerRequests.ToListAsync();
        }

        // GET: api/CustomerRequests/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerRequest>> GetCustomerRequest(string id)
        {
            var customerRequest = await _context.CustomerRequests.FindAsync(id);

            if (customerRequest == null)
            {
                return NotFound();
            }

            return customerRequest;
        }

        // PUT: api/CustomerRequests/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomerRequest(string id, CustomerRequestDTO customerRequestDTO)
        {
            var primaryGemstone = await _context.Gemstones
                    .Where(g =>
                        g.Name == customerRequestDTO.PrimaryGemstone.Name &&
                        g.Clarity == customerRequestDTO.PrimaryGemstone.Clarity &&
                        g.Color == customerRequestDTO.PrimaryGemstone.Color &&
                        g.Shape == customerRequestDTO.PrimaryGemstone.Shape &&
                        g.Size == customerRequestDTO.PrimaryGemstone.Size &&
                        g.Cut == customerRequestDTO.PrimaryGemstone.Cut &&
                        g.Price == customerRequestDTO.PrimaryGemstone.Price &&
                        g.ProductSample == null && g.CustomizeRequestId == null)
                    .FirstOrDefaultAsync();

            if (primaryGemstone == null)
            {
                return BadRequest("The primary gemstone was not found.");
            }

            var additionalGemstones = await _context.Gemstones
                .Where(g => customerRequestDTO.AdditionalGemstoneNames.Contains(g.Name) && g.CaratWeight >= 0.1 && g.CaratWeight <= 0.3)
                .GroupBy(g => g.Name)
                .Select(g => g.FirstOrDefault())
                .OrderBy(_ => Guid.NewGuid())
                .Take(2)
                .ToListAsync();
            var allSelectedGemstones = new List<Gemstone> { primaryGemstone }.Concat(additionalGemstones).ToList();
            var gold = await _context.Golds
            .FirstOrDefaultAsync(g => g.GoldType == customerRequestDTO.GoldType);

            if (gold == null)
            {
                return BadRequest("Gold type not found.");
            }
            var updateCusReq = await _context.CustomerRequests.FindAsync(id);
            updateCusReq.GoldId = gold.GoldId;
            updateCusReq.CustomerId = customerRequestDTO.CustomerId;
            updateCusReq.SaleStaffId = customerRequestDTO.SaleStaffId;
            updateCusReq.ManagerId = customerRequestDTO.ManagerId;
            updateCusReq.Type = customerRequestDTO.Type;
            updateCusReq.Style = customerRequestDTO.Style;
            updateCusReq.Size = customerRequestDTO.Size;
            updateCusReq.Quantity = customerRequestDTO.Quantity;
            updateCusReq.Status = customerRequestDTO.Status;
            updateCusReq.Gemstones = allSelectedGemstones;
            updateCusReq.Gold = gold;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerRequestExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
        [HttpGet("customize-form-template")]
        public ActionResult<CustomerRequestDTO> GetCustomizeFormTemplate([FromQuery] string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return BadRequest("Type must be provided.");
            }

            var template = new CustomerRequestDTO
            {
                Type = type
            };

            return Ok(template);
        }

        // POST: api/CustomerRequests
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<CustomerRequest>> PostCustomerRequest(CustomerRequestDTO customerRequestDTO)
        {
            var primaryGemstone = await _context.Gemstones
                    .Where(g =>
                        g.Name == customerRequestDTO.PrimaryGemstone.Name &&
                        g.Clarity == customerRequestDTO.PrimaryGemstone.Clarity &&
                        g.Color == customerRequestDTO.PrimaryGemstone.Color &&
                        g.Shape == customerRequestDTO.PrimaryGemstone.Shape &&
                        g.Size == customerRequestDTO.PrimaryGemstone.Size &&
                        g.Cut == customerRequestDTO.PrimaryGemstone.Cut &&
                        g.Price == customerRequestDTO.PrimaryGemstone.Price &&
                        g.ProductSample == null && g.CustomizeRequestId == null)
                    .FirstOrDefaultAsync();

            if (primaryGemstone == null)
            {
                return BadRequest("The primary gemstone was not found.");
            }

            var additionalGemstones = await _context.Gemstones
                .Where(g => customerRequestDTO.AdditionalGemstoneNames.Contains(g.Name) && g.CaratWeight >= 0.1 && g.CaratWeight <= 0.3)
                .GroupBy(g => g.Name)
                .Select(g => g.FirstOrDefault())
                .OrderBy(_ => Guid.NewGuid())
                .Take(2)
                .ToListAsync();
            var allSelectedGemstones = new List<Gemstone> { primaryGemstone }.Concat(additionalGemstones).ToList();
            var gold = await _context.Golds
            .FirstOrDefaultAsync(g => g.GoldType == customerRequestDTO.GoldType);

            if (gold == null)
            {
                return BadRequest("Gold type not found.");
            }
            var uniqueId = await IdGenerator.GenerateUniqueId<CustomerRequest>(_context, "REQ", 3);

            var customerRequest = new CustomerRequest
            {
                CustomizeRequestId = uniqueId,
                GoldId = gold.GoldId,
                CustomerId = customerRequestDTO.CustomerId,
                Type = customerRequestDTO.Type,
                Style = customerRequestDTO.Style,
                Size = customerRequestDTO.Size,
                Quantity = customerRequestDTO.Quantity,
                Gemstones = allSelectedGemstones,
                Gold = gold,
                Status = "Pending"
            };
            _context.CustomerRequests.Add(customerRequest);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (CustomerRequestExists(customerRequest.CustomizeRequestId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetCustomerRequest", new { id = customerRequest.CustomizeRequestId }, customerRequest);
        }

        // DELETE: api/CustomerRequests/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomerRequest(string id)
        {
            var customerRequest = await _context.CustomerRequests.FindAsync(id);
            if (customerRequest == null)
            {
                return NotFound();
            }

            _context.CustomerRequests.Remove(customerRequest);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CustomerRequestExists(string id)
        {
            return _context.CustomerRequests.Any(e => e.CustomizeRequestId == id);
        }
        [HttpGet("paging")]
        public async Task<IActionResult> GetAllPaging([FromQuery] OrderPagingRequest request)
        {
            var products = await _requestService.GetAllPaging(request);
            return Ok(products);
        }
        [HttpGet("prefill")]
        public async Task<IActionResult> PrefillCustomizeRequest([FromQuery] string productSampleId)
        {
            var productSample = await _context.ProductSamples
                .Where(ps => ps.ProductSampleId == productSampleId)
                .Select(ps => new
                {
                    Type = ps.Type,
                    Style = ps.Style,
                    Quantity = 1, // Default quantity, adjust as necessary
                    PrimaryGemstone = ps.Gemstones
                        .Where(g => g.CaratWeight > 0.3)
                        .Select(g => new AddGemstoneDTO
                        {
                            Name = g.Name,
                            Clarity = g.Clarity,
                            Color = g.Color,
                            Shape = g.Shape,
                            Size = g.Size,
                            Cut = g.Cut,
                            CaratWeight = g.CaratWeight,
                        }).FirstOrDefault(),
                        AdditionalGemstones = ps.Gemstones
                        .Where(g => g.CaratWeight <= 0.3)
                        .Select(g => new Gemstone
                        {
                            Name = g.Name,
                            Clarity = g.Clarity,
                            Color = g.Color,
                            Shape = g.Shape,
                            Size = g.Size,
                            Cut = g.Cut,
                            CaratWeight = g.CaratWeight,
                        })
                        .ToList(),
                    GoldType = ps.Gold.GoldType,
                })
                .FirstOrDefaultAsync();

            if (productSample == null)
            {
                return NotFound("Product sample not found.");
            }

            var customerRequest = new CustomerRequestDTO
            {
                Type = productSample.Type,
                Style = productSample.Style,
                Quantity = productSample.Quantity,
                PrimaryGemstone = productSample.PrimaryGemstone,
                AdditionalGemstoneNames = productSample.AdditionalGemstones.Select(g => g.Name).ToList(),
                GoldType = productSample.GoldType,
            };

            return Ok(customerRequest);
        }
        [HttpPost("approve/{customizeRequestId}")]
        public async Task<IActionResult> ApproveCustomerRequest(string customizeRequestId)
        {
            var customerRequest = await _context.CustomerRequests
                .Include(cr => cr.Gemstones)
                .Include(cr => cr.Gold)
                .FirstOrDefaultAsync(cr => cr.CustomizeRequestId == customizeRequestId);

            if (customerRequest == null)
            {
                return NotFound("Customer request not found.");
            }

            customerRequest.Status = "Approved";
            var request = await _context.ApprovalRequests
                .Where(ar => ar.CustomerRequestId == customizeRequestId && ar.Status == "Approved")
                .FirstOrDefaultAsync();

            var order = new Order
            {
                OrderId = await IdGenerator.GenerateUniqueId<Order>(_context, "ORD", 6),
                ProductionStaffId = null, 
                OrderDate = DateTime.Now,
                DepositAmount = request.Price *0.3M, 
                Status = "Pending",
                CustomizeRequestId = customerRequest.CustomizeRequestId,
                PaymentMethodId = "1",
                TotalPrice = request.Price
            };

            _context.Orders.Add(order);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            return Ok(order);
        }
        [HttpDelete("reject/{customizeRequestId}")]
        public async Task<IActionResult> RejectCustomerRequest(string customizeRequestId)
        {
            var customerRequest = await _context.CustomerRequests
                .Include(cr => cr.Gemstones)
                .FirstOrDefaultAsync(cr => cr.CustomizeRequestId == customizeRequestId);
            if (customerRequest == null)
            {
                return NotFound("Customer request not found.");
            }
            foreach (var gemstone in customerRequest.Gemstones)
            {
                gemstone.CustomizeRequestId = null;
            }
            _context.CustomerRequests.Remove(customerRequest);
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            return NoContent();
        }
        [HttpGet("{customerRequestId}/quotations")]
        public async Task<IActionResult> GetCustomerRequestQuotations(string customerRequestId)
        {
            var customerRequest = await _requestService.GetCustomerRequestWithQuotationsAsync(customerRequestId);

            if (customerRequest == null)
            {
                return NotFound();
            }

            var response = new
            {
                customerRequest.quotation,
                customerRequest.quotationDes
            };

            return Ok(response);
        }
        private string GetCurrentUserId()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sid);
            return userId;
        }
    }
}