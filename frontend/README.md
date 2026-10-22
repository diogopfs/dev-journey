# DevJourney — Frontend (Angular)

Veja o [README principal](../README.md) para a visão geral do projeto.

```bash
npm install
npm start          # http://localhost:4200 (proxy de /api para http://localhost:5000)
npm test           # testes unitários com Vitest
npm run build      # build de produção em dist/
```

## Organização

```
src/app/
├── core/          # Singletons transversais: autenticação, interceptors, guards, tratamento de erros HTTP
├── features/      # Uma pasta por funcionalidade (auth, users, audit), com componentes carregados sob demanda
└── layout/        # Estrutura visual das páginas autenticadas (menu + conteúdo)
```

Recursos modernos do Angular utilizados: standalone components, Signals (`signal`, `computed`),
`rxResource`, novo control flow (`@if`, `@for`), `input()` ligado a parâmetros de rota,
interceptors e guards funcionais, `provideAppInitializer`, aplicação zoneless e lazy loading de rotas.
