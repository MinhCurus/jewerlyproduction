﻿using JewelryProduction.DbContext;
using JewelryProduction.DTO;
using JewelryProduction.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JewelryProduction.Repositories
{
    public class _3dDesignRepository : I3dDesignRepository
    {
        private readonly JewelryProductionContext _context;
        private readonly UserManager<AppUser> _userManager;

        public _3dDesignRepository(JewelryProductionContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task AddDesignAsync(_3ddesign design)
        {
            _context._3ddesigns.Add(design);
            await _context.SaveChangesAsync();
        }

        public async Task<AppUser> FindUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<Get3dDesignDTO> Get_3Ddesign(string id)
        {
            var design = await _context._3ddesigns
                .Include(d => d.Order)
                .Include(d => d.ProductSample)
                .Include(d => d.DesignStaff)
                .Where(d => d._3dDesignId.Equals(id))
                .FirstOrDefaultAsync();

            if (design == null)
            {
                throw new Exception($"Design with ID {id} not found.");
            }

            var result = new Get3dDesignDTO
            {
                _3dDesignId = design._3dDesignId,
                DesignName = design.DesignName,
                Image = design.Image,
                OrderId = design.OrderId,
                ProductSampleId = design.ProductSampleId,
                DesignStaffId = design.DesignStaffId
            };

            return result;
        }

        public async Task<List<Get3dDesignDTO>> Get_3Ddesigns()
        {
            var designs = await _context._3ddesigns
                .Include(d => d.Order)
                .Include(d => d.ProductSample)
                .Include(d => d.DesignStaff)
                .ToListAsync();

            var result = designs.Select(design => new Get3dDesignDTO
            {
                _3dDesignId = design._3dDesignId,
                DesignName = design.DesignName,
                Image = design.Image,
                OrderId = design.OrderId,
                ProductSampleId = design.ProductSampleId,
                DesignStaffId = design.DesignStaffId
            }).ToList();

            return result;
        }

        public async Task DeleteDesignAsync(string id)
        {
            var design = await _context._3ddesigns.FindAsync(id);

            if (design == null)
            {
                throw new Exception($"Design with ID {id} not found.");
            }

            _context._3ddesigns.Remove(design);
            await _context.SaveChangesAsync();  
        }

        public async Task UpdateProductSample(string designId, string sampleId)
        {
            var design = await _context._3ddesigns.FindAsync(designId);
            if (design.ProductSampleId != null)
                throw new Exception($"The design is already implemented into another sample");
            design.ProductSampleId= sampleId;
            await _context.SaveChangesAsync();
        }
    }
}