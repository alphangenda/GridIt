# Auth Improvements — Design Spec
Date: 2026-05-02

## Overview
Four focused improvements to the authentication flow in GridIt.

---

## Feature 1 — 2FA: Ne plus redemander pendant 30 jours

**Fichiers touchés :**
- `src/Web/appsettings.Development.json`
- `src/Web/appsettings.json`

**Changement :** Ajouter `"TwoFactorAuthenticationDayDelay": 30` dans la section `"Application"` des deux fichiers.

**Pourquoi ça marche :** La logique backend existe déjà dans `AuthenticationService.GetTwoFactorAuthenticationTokenCodeUserWithPassword()` — si `LastTwoFactorAuthenticationWasLessThanGivenNumberOfDaysAgo(30)` est vrai, retourne `null` (pas de 2FA). La valeur par défaut `0` désactivait effectivement ce mécanisme.

---

## Feature 2 — Input 2FA: 6 carrés OTP

**Fichiers touchés :**
- `src/Web/vue-app/src/components/forms/OtpInput.vue` *(nouveau)*
- `src/Web/vue-app/src/views/TwoFactor.vue`

**Composant `OtpInput.vue` :**
- Props : `modelValue: string` (v-model, string de 6 chiffres)
- 6 `<input type="text" inputmode="numeric" maxlength="1">` dans une rangée flex
- Auto-avance : à la saisie d'un chiffre, focus sur le champ suivant
- Backspace sur champ vide : retour au champ précédent
- Paste : si 6 chiffres détectés sur `paste` event → répartir sur les 6 champs
- Émet `update:modelValue` avec la concaténation des 6 champs

**`TwoFactor.vue` :** remplace le `<FormInput>` par `<OtpInput v-model="code" />`. La validation `required` reste sur le bouton submit (code.length === 6).

---

## Feature 3 — Bug ConfirmEmail: mauvais layout

**Fichier touché :**
- `src/Web/vue-app/src/App.vue`

**Cause :** `authenticationRoutes` (ligne 18) ne contient pas `'confirmEmail'` ni `'register'`. Si Pinia a un user persisté, `DashboardLayout` s'affiche sur ces pages.

**Fix :** Ajouter `'confirmEmail'` et `'register'` dans le tableau `authenticationRoutes`.

---

## Feature 4 — Inscription silencieuse si compte existant

**Fichier touché :**
- `src/Web/Features/Public/Authentication/Register/RegisterEndPoint.cs`

**Changement :** Remplacer le bloc `if (_userRepository.UserWithEmailExists(req.Email))` qui retourne une erreur `EmailAlreadyExists` par un retour `SucceededOrNotResponse(true)` sans envoyer d'email.

**Comportement :** Le frontend reçoit `succeeded: true` → redirige vers `/login` comme si l'inscription avait réussi. Aucun email envoyé. L'utilisateur ne peut pas deviner si un compte existait.

---

## Périmètre

Aucune migration de base de données. Aucune nouvelle dépendance npm. Un seul nouveau fichier Vue. Les autres changements sont des modifications ciblées dans des fichiers existants.
