namespace UserService.Application.Dtos.Keycloak;

public record UsersPagedKeycloak(
    List<UserResponseKeycloak> Users,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);