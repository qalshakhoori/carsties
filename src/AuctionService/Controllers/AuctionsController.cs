using AuctionService.Data;
using AuctionService.Dtos;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
  private readonly AuctionDbContext _context;
  private readonly IMapper _mapper;

  public AuctionsController(AuctionDbContext context, IMapper mapper)
  {
    _context = context;
    _mapper = mapper;
  }

  [HttpGet]
  public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions()
  {
    var auctions = await _context.Auctions
    .Include(x => x.Item)
    .OrderBy(x => x.Item.Make)
    .ToListAsync();


    return _mapper.Map<List<AuctionDto>>(auctions);
  }

  [HttpGet("{id}")]
  public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
  {
    var auction = await _context.Auctions
      .Include(x => x.Item)
      .FirstOrDefaultAsync(x => x.Id == id);

    if (auction == null)
      return NotFound();

    return _mapper.Map<AuctionDto>(auction);
  }

  [HttpPost]
  public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAutionDto dto)
  {
    var auction = _mapper.Map<Auction>(dto);

    // TODO: add current user as seller

    auction.Seller = "test";

    _context.Auctions.Add(auction);

    var success = await _context.SaveChangesAsync() > 0;

    if (!success)
      return BadRequest("Could not save changes to the DB");

    return CreatedAtAction(nameof(GetAuctionById),
      new { auction.Id },
     _mapper.Map<AuctionDto>(auction));
  }

  [HttpPut("{id}")]
  public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto dto)
  {
    var auction = await _context.Auctions
    .Include(m => m.Item)
    .FirstOrDefaultAsync(m => m.Id == id);

    if (auction == null)
      return NotFound();

    // TODO: check seller === username

    auction.Item.Make = dto.Make ?? auction.Item.Make;
    auction.Item.Model = dto.Model ?? auction.Item.Model;
    auction.Item.Color = dto.Color ?? auction.Item.Color;
    auction.Item.Mileage = dto.Mileage ?? auction.Item.Mileage;
    auction.Item.Year = dto.Year ?? auction.Item.Year;

    var success = await _context.SaveChangesAsync() > 0;

    if (success)
      return Ok();

    return BadRequest("Problem while saving changes");
  }

  [HttpDelete("{id}")]
  public async Task<ActionResult> DeleteAuction(Guid id)
  {
    var auction = await _context.Auctions.FindAsync(id);

    if (auction == null)
      return NotFound();

    // TODO: check seller == username

    _context.Auctions.Remove(auction);

    var success = await _context.SaveChangesAsync() > 0;

    if (!success)
      return BadRequest("Could not update DB");

    return Ok();
  }
}