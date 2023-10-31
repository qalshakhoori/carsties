using AuctionService.Dtos;
using AuctionService.Entities;
using AutoMapper;
using Contracts;

namespace AuctionService.RequestHelpers;

public class MappingProfiles : Profile
{
  public MappingProfiles()
  {
    CreateMap<Auction, AuctionDto>().IncludeMembers(x => x.Item);
    CreateMap<Item, AuctionDto>();
    CreateMap<CreateAutionDto, Auction>()
    .ForMember(d => d.Item, o => o.MapFrom(s => s));
    CreateMap<CreateAutionDto, Item>();
    CreateMap<UpdateAuctionDto, Item>();
    CreateMap<AuctionDto, AuctionCreated>();
    CreateMap<Auction, AuctionUpdated>().IncludeMembers(x => x.Item);
    CreateMap<Item, AuctionUpdated>();
  }
}