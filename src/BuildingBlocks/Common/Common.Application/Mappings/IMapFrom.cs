using AutoMapper;

namespace Common.Application.Mappings;

/// <summary>
/// Interface para configuração de mapeamento AutoMapper
/// </summary>
public interface IMapFrom<T>
{
    void Mapping(Profile profile) => profile.CreateMap(typeof(T), GetType());
}