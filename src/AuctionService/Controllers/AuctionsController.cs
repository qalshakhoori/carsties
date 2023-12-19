using AuctionService.Dtos;
using AuctionService.Entities;
using AutoMapper;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
  private readonly IAuctionRepo _repo;
  private readonly IMapper _mapper;
  private readonly IPublishEndpoint _publishEndpoint;

  public AuctionsController(IAuctionRepo repo, IMapper mapper, IPublishEndpoint publishEndpoint)
  {

    _repo = repo;
    _mapper = mapper;
    _publishEndpoint = publishEndpoint;
  }

  [HttpGet]
  public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
  {
    return await _repo.GetAuctionsAsync(date);
  }

  [HttpGet("{id}")]
  public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
  {
    var auction = await _repo.GetAuctionByIdAsync(id);

    if (auction == null)
      return NotFound();

    return auction;
  }

  [Authorize]
  [HttpPost]
  public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto dto)
  {
    var auction = _mapper.Map<Auction>(dto);

    // TODO: add current user as seller

    auction.Seller = User.Identity.Name; // have to set NameClaimType on Program.cs to be able to get username from User identity

    // After adding Entity Framework Outbox
    // This block will work as a transaction in entity framework while saving changes to database,
    // Either all success or all fail
    _repo.AddAuction(auction);

    var newAuction = _mapper.Map<AuctionDto>(auction);

    await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

    var success = await _repo.SaveChangesAsync();
    // End of Block
    if (!success)
      return BadRequest("Could not save changes to the DB");

    return CreatedAtAction(nameof(GetAuctionById),
      new { auction.Id }, newAuction);
  }

  [Authorize]
  [HttpPut("{id}")]
  public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto dto)
  {
    var auction = await _repo.GetAuctionEntityById(id);

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

    var success = await _repo.SaveChangesAsync();

    if (success)
      return Ok();

    return BadRequest("Problem while saving changes");
  }

  [Authorize]
  [HttpDelete("{id}")]
  public async Task<ActionResult> DeleteAuction(Guid id)
  {
    var auction = await _repo.GetAuctionEntityById(id);

    if (auction == null)
      return NotFound();

    if (auction.Seller != User.Identity.Name)
      return Forbid();

    _repo.RemoveAuction(auction);

    var auctionDeleted = new AuctionDeleted { Id = id.ToString() };

    await _publishEndpoint.Publish(auctionDeleted);

    var success = await _repo.SaveChangesAsync();

    if (!success)
      return BadRequest("Could not update DB");

    return Ok();
  }
}