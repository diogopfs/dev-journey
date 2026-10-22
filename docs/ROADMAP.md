# Roadmap da jornada

Cada etapa evolui este mesmo repositório. A ideia é marcar cada etapa concluída com uma tag git
(`etapa-01`, `etapa-02`...) para poder comparar o "antes e depois".

## ✅ Etapa 1 — Fundação
- [x] Clean Architecture + SOLID (Domain, Application, Infrastructure, Api, Worker)
- [x] CRUD de usuários (Angular 22 + .NET 10)
- [x] JWT + refresh token rotativo em cookie HttpOnly, perfis e políticas, rate limiting
- [x] PostgreSQL (EF Core + migrations) e MongoDB (auditoria)
- [x] RabbitMQ: exchange topic, duas filas, DLQ, ack manual, consumidor idempotente
- [x] OpenTelemetry (traces distribuídos, métricas, logs) + Aspire Dashboard
- [x] Testes unitários (xUnit, NSubstitute, Shouldly, Vitest)
- [x] Docker Compose e CI com GitHub Actions

## 🔜 Sugestões para as próximas etapas
1. **Testes de integração** com `WebApplicationFactory` + **Testcontainers** (Postgres, Mongo e RabbitMQ reais nos testes).
2. **Transactional Outbox**: garantir que nenhum evento seja perdido se o broker cair.
3. **Retry com backoff** e reprocessamento da DLQ; política de resiliência HTTP com **Polly** / `Microsoft.Extensions.Resilience`.
4. **OAuth2 / OpenID Connect de verdade**: Keycloak (ou OpenIddict) com *Authorization Code + PKCE*, login social, MFA.
5. **Cache distribuído** com Redis (`HybridCache`) na listagem de usuários.
6. **Kafka** lado a lado com o RabbitMQ para entender log de eventos x fila de mensagens.
7. **.NET Aspire AppHost** para orquestrar o ambiente local em vez do Docker Compose.
8. **Grafana + Prometheus + Loki + Tempo** como stack de observabilidade alternativa; alertas.
9. **Kubernetes** (kind/minikube) com Helm e deploy contínuo.
10. **Deploy na nuvem** em serviços gratuitos (Azure Container Apps / Render / Fly.io + Neon + Atlas + CloudAMQP).
11. Front-end: Angular **Signal Forms**, testes E2E com Playwright, internacionalização, PWA.
12. Análise de qualidade e segurança: SonarCloud, Dependabot, CodeQL, OWASP ZAP.
