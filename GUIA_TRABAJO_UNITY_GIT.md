# Guia De Trabajo Con `Scene_A.unity`

Esta guia es para evitar que vuelvan a salir conflictos como los de los PR `#26` y `#28`.

## 1. Reglas Del Equipo

1. Todos usamos la misma version de Unity: `2022.3.62f3`.
2. `Scene_A.unity` es la escena principal, asi que antes de tocarla hay que avisar en el chat que zona, objetos o sistemas se van a modificar.
3. Si dos personas necesitan mover la misma zona, el mismo prefab o los mismos managers, una sola integra esos cambios.
4. Los conflictos de archivos `.unity`, `.prefab`, `.asset` y `.meta` no se resuelven desde GitHub web. Siempre se resuelven en local.
5. Antes de abrir un PR, la rama debe actualizarse con `main`.
6. Los commits de escena deben ser pequenos y frecuentes. No conviene juntar todo en un solo commit gigante.

## 2. Configuracion Inicial De Cada Integrante

Cada companero debe correr esto una sola vez en su maquina. Si Unity esta instalado en otra carpeta, solo hay que cambiar la ruta del `.exe`.

```powershell
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "\"C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/Tools/UnityYAMLMerge.exe\" merge -h -p --force --fallback none %O %B %A %A"
git config --global merge.tool unityyamlmerge
git config --global mergetool.unityyamlmerge.cmd "\"C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/Tools/UnityYAMLMerge.exe\" merge -h -p --force --fallback none \"$BASE\" \"$REMOTE\" \"$LOCAL\" \"$MERGED\""
git config --global mergetool.unityyamlmerge.trustExitCode true
git config --global mergetool.keepBackup false
```

Para comprobar que quedo bien configurado:

```powershell
git config --global --get merge.unityyamlmerge.driver
```

## 3. Flujo Normal De Trabajo

1. Actualiza `main`.
2. Crea tu rama desde `main`.
3. Trabaja en Unity.
4. Haz commits pequenos.
5. Antes del PR, vuelve a meter `main` en tu rama.
6. Si hay conflicto en la escena, se resuelve localmente.

Comandos:

```powershell
git checkout main
git pull origin main
git checkout -b nombre-de-mi-rama
```

Despues de trabajar:

```powershell
git add .
git commit -m "Descripcion corta del cambio"
git push origin nombre-de-mi-rama
```

## 4. Paso Obligatorio Antes Del PR

Antes de abrir un pull request:

```powershell
git fetch origin
git checkout nombre-de-mi-rama
git merge origin/main
```

Si no hubo conflicto:

```powershell
git push origin nombre-de-mi-rama
```

## 5. Que Hacer Si Hay Conflicto En `Scene_A.unity`

1. No tocar el boton `Resolve conflicts` de GitHub.
2. Resolver el merge en local.
3. Abrir el proyecto en Unity.
4. Revisar que `Scene_A` abra sin errores de parseo, sin prefabs rotos y sin objetos perdidos.
5. Guardar la escena.
6. Hacer commit del merge y push.

Comandos:

```powershell
git fetch origin
git checkout nombre-de-mi-rama
git merge origin/main
```

Si Git marca conflicto en `Scene_A.unity`, primero se resuelve localmente y despues:

```powershell
git add Assets/Flooded_Grounds/Scenes/Scene_A.unity
git commit
git push origin nombre-de-mi-rama
```

## 6. Reglas Practicas Para Bajar Conflictos

1. Avisar que parte de `Scene_A` va a tocar cada quien.
2. Evitar mover o borrar objetos que otro companero este usando.
3. Si se agregan objetos reutilizables, convertirlos en prefabs.
4. Si se va a tocar UI global, terreno, luces principales o managers, avisarlo antes porque eso choca muy seguido.
5. Despues de que se mergea un PR, todos deben actualizar su `main` antes de seguir trabajando.

## 7. Resumen Corto

Si van a editar `Scene_A.unity`, el flujo correcto es:

1. `git pull origin main`
2. trabajar en rama propia
3. `git merge origin/main` antes del PR
4. resolver conflictos solo en local
5. abrir Unity y revisar la escena antes del push

Si no se hace ese paso de actualizar la rama antes del PR, el conflicto en la escena va a volver a aparecer.
