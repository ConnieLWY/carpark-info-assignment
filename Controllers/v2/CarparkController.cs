using Microsoft.AspNetCore.Mvc;
using HandshakesByDC_BEAssignment.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace HandshakesByDC_BEAssignment.Controllers.v2
{
    [Authorize]
    [ApiController]
    [Route("api/v2/[controller]")]
    public class CarparkController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CarparkController> _logger;

        public CarparkController(AppDbContext context, ILogger<CarparkController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("free-parking")]
        public async Task<ActionResult<IEnumerable<Carpark>>> GetCarparksWithFreeParking()
        {
            try
            {
                var carparks = await _context.Carparks
                    .Where(c => c.FreeParking.ToUpper().Contains("YES") ||
                               c.FreeParking.ToUpper().Contains("SUN") ||
                               c.FreeParking.ToUpper().Contains("PUBLIC HOLIDAY"))
                    .ToListAsync();

                return Ok(carparks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving carparks with free parking: {ex.Message}");
                return StatusCode(500, "Internal server error while retrieving carparks");
            }
        }

        [HttpGet("night-parking")]
        public async Task<ActionResult<IEnumerable<Carpark>>> GetCarparksWithNightParking()
        {
            try
            {
                var carparks = await _context.Carparks
                    .Where(c => c.NightParking)
                    .ToListAsync();

                return Ok(carparks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving carparks with night parking: {ex.Message}");
                return StatusCode(500, "Internal server error while retrieving carparks");
            }
        }

        [HttpGet("by-height/{heightInMeters}")]
        public async Task<ActionResult<IEnumerable<Carpark>>> GetCarparksByHeight(float heightInMeters)
        {
            try
            {
                if (heightInMeters <= 0)
                {
                    return BadRequest("Vehicle height must be greater than 0 meters");
                }

                var carparks = await _context.Carparks
                    .Where(c => c.GantryHeight >= heightInMeters)
                    .OrderBy(c => c.GantryHeight)
                    .ToListAsync();

                return Ok(carparks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving carparks by height requirement: {ex.Message}");
                return StatusCode(500, "Internal server error while retrieving carparks");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Carpark>>> SearchCarparks(
            [FromQuery] bool? freeParking,
            [FromQuery] bool? nightParking,
            [FromQuery] float? minHeight)
        {
            try
            {
                var query = _context.Carparks.AsQueryable();

                if (freeParking == true)
                {
                    query = query.Where(c => c.FreeParking.ToUpper().Contains("YES") ||
                                           c.FreeParking.ToUpper().Contains("SUN") ||
                                           c.FreeParking.ToUpper().Contains("PUBLIC HOLIDAY"));
                }

                if (nightParking == true)
                {
                    query = query.Where(c => c.NightParking);
                }

                if (minHeight.HasValue && minHeight > 0)
                {
                    query = query.Where(c => c.GantryHeight >= minHeight);
                }

                var carparks = await query
                    .OrderBy(c => c.CarparkNo)
                    .ToListAsync();

                return Ok(carparks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching carparks: {ex.Message}");
                return StatusCode(500, "Internal server error while searching carparks");
            }
        }
    }
}