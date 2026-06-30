# Twisted 3v3 — MOBA Unity 6

MOBA 3v3 rapide (parties 12–18 min) inspiré de l'ancien Twisted Treeline.
Architecture **data-driven** : la logique vit dans des composants MonoBehaviour modulaires,
les données (champions, capacités, items, camps) vivent dans des **ScriptableObjects**.

## Stack
- Unity 6 (6000.5.1f1)
- C# moderne, séparation des préoccupations (composition de composants + SO en data layer)
- Pilotage éditeur via MCP for Unity

## Structure des dossiers

```
3v3/
└── Assets/
    └── _Game/
        ├── Art/                     # Modèles, matériaux, VFX, UI sprites
        ├── Audio/
        ├── Prefabs/
        │   ├── Champions/
        │   ├── Abilities/           # Projectiles, zones, VFX
        │   └── Jungle/
        ├── ScriptableObjects/       # ASSETS de données (instances .asset)
        │   ├── Champions/
        │   ├── Abilities/
        │   ├── Items/
        │   └── Jungle/
        ├── Scenes/
        │   ├── Bootstrap.unity      # Point d'entrée
        │   └── Map_3v3.unity        # La map : 2 lanes + jungle + Autel
        └── Scripts/
            ├── Core/                # Enums, constantes, GameManager, services
            ├── Stats/              # Système de stats (Stat, modifiers, health)
            ├── Champions/          # Champion, contrôleur, factory
            ├── Abilities/          # Ability System (cœur du gameplay)
            │   └── Effects/        # Effets réutilisables (dégâts, CC, soin...)
            ├── Combat/             # Damage, targeting, équipes
            ├── Items/              # Items & inventaire
            ├── Jungle/             # Camps, monstres, Autel des Âmes
            └── _Game.asmdef        # Assembly dédiée (compilation rapide)
```

## Architecture des capacités (résumé)

- `AbilityData` (ScriptableObject abstrait) = **données + comportement** d'un sort.
- `AbilityInstance` = **état runtime** par champion/slot (cooldown, rang, charges).
- `AbilitySystem` (MonoBehaviour) = orchestrateur : input → cast → tick des cooldowns.
- `AbilityContext` = paquet de données passé à l'exécution (lanceur, cible, point sol...).
- Slots : `Passive`, `Q`, `Z`, `E`, `R`.

## Roadmap
Développement **champion par champion, feature par feature**.
Ordre conseillé : Stats → Health/Combat → AbilitySystem → Kaelthar complet → autres champions → Jungle → Items.
