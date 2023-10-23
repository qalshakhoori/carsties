using AuctionService.Dtos;
using AuctionService.Entities;
using AutoMapper;

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
  }
}