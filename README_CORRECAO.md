# MiyagiStrap — correção das FastFlags

## O que foi corrigido

- O arquivo `ClientAppSettings.json` agora é gravado em um temporário e validado antes de substituir o arquivo ativo.
- O launcher confirma que o arquivo foi criado antes de iniciar o Roblox.
- O status informa quantas flags foram aplicadas.
- Valores escritos como texto, por exemplo `"250"`, são convertidos automaticamente para números.
- O fluxo continua usando `ClientSettings\ClientAppSettings.json`; não há injeção em processo.

## Formato recomendado

Prefira números e booleanos sem aspas:

```json
{
  "DFIntS2PhysicsSenderRate": 250,
  "DFIntRakNetLoopMs": 1,
  "DFIntDebugDynamicRenderKiloPixels": 2074
}
```

## Como testar no Windows

1. Compile com `Publicar.bat` ou pelo Visual Studio.
2. Abra o MiyagiStrap.
3. Configure as flags e clique em **Salvar**.
4. Feche qualquer Roblox aberto.
5. Clique em **Jogar Roblox**.
6. Confirme se aparece `N flag(s) aplicadas`.
7. Verifique o arquivo em:

```text
%LOCALAPPDATA%\MiyagiStrap\Versions\<versão>\ClientSettings\ClientAppSettings.json
```

A existência do arquivo confirma que o launcher aplicou a configuração. Se ele existir, estiver com números corretos e ainda assim uma flag não surtir efeito, o cliente atual provavelmente ignora ou bloqueia essa flag.
