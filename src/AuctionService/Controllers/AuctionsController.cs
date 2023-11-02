using AuctionService.Data;
using AuctionService.Dtos;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
  private readonly AuctionDbContext _context;
  private readonly IMapper _mapper;
  private readonly IPublishEndpoint _publishEndpoint;

  public AuctionsController(AuctionDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
  {
    _context = context;
    _mapper = mapper;
    _publishEndpoint = publishEndpoint;
  }

  [HttpGet]
  public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
  {
    var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

    if (!string.IsNullOrEmpty(date))
      query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);

    return await query
      .ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
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

  [Authorize]
  [HttpPost]
  public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAutionDto dto)
  {
    var auction = _mapper.Map<Auction>(dto);

    // TODO: add current user as seller

    auction.Seller = User.Identity.Name; // have to set NameClaimType on Program.cs to be able to get username from User identity

    // After adding Entity Framework Outbox
    // This block will work as a transaction in entity framework while saving changes to database,
    // Either all success or all fail
    _context.Auctions.Add(auction);

    var newAuction = _mapper.Map<AuctionDto>(auction);

    await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

    var success = await _context.SaveChangesAsync() > 0;
    // End of Block
    if (!success)
      return BadRequest("Could not save changes to the DB");

    return CreatedAtAction(nameof(GetAuctionById),
      new { auction.Id },
     _mapper.Map<AuctionDto>(auction));
  }

  [Authorize]
  [HttpPut("{id}")]
  public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto dto)
  {
    var auction = await _context.Auctions
    .Include(m => m.Item)
    .FirstOrDefaultAsync(m => m.Id == id);

    if (auction == null)
      return NotFound();

    if (auction.Seller != User.Identity.Name)
      return Forbid();

    auction.Item.Make = dto.Make ?? auction.Item.Make;
    auction.Item.Model = dto.Model ?? auction.Item.Model;
    auction.Item.Color = dto.Color ?? auction.Item.Color;
    auction.Item.Mileage = dto.Mileage ?? auction.Item.Mileage;
    auction.Item.Year = dto.Year ?? auction.Item.Year;

    var auctionUpdated = _mapper.Map<AuctionUpdated>(auction);

    await _publishEndpoint.Publish(auctionUpdated);

    var success = await _context.SaveChangesAsync() > 0;

    if (success)
      return Ok();

    return BadRequest("Problem while saving changes");
  }

  [Authorize]
  [HttpDelete("{id}")]
  public async Task<ActionResult> DeleteAuction(Guid id)
  {
    var auction = await _context.Auctions.FindAsync(id);

    if (auction == null)
      return NotFound();

    if (auction.Seller != User.Identity.Name)
      return Forbid();

    _context.Auctions.Remove(auction);

    var auctionDeleted = new AuctionDeleted { Id = id.ToString() };

    await _publishEndpoint.Publish(auctionDeleted);

    var success = await _context.SaveChangesAsync() > 0;

    if (!success)
      return BadRequest("Could not update DB");

    return Ok();
  }
}