# User Service API

## Base URL
`/api/users`

## Endpoints

### Auth
*   `POST /api/auth/register` - Registrar novo usuário
*   `POST /api/auth/login` - Autenticar usuário
*   `POST /api/auth/refresh-token` - Renovar token JWT (Refresh Token)

### Profiles
*   `GET /api/profiles/me` - Obter perfil do usuário logado
*   `PUT /api/profiles/me` - Atualizar perfil

### Addresses
*   `GET /api/addresses` - Listar endereços
*   `POST /api/addresses` - Adicionar endereço
*   `PUT /api/addresses/{id}` - Atualizar endereço
*   `DELETE /api/addresses/{id}` - Remover endereço

### Notifications
*   `GET /api/notifications` - Listar notificações
*   `PUT /api/notifications/{id}/read` - Marcar como lida
