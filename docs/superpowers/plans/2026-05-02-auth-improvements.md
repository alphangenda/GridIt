# Auth Improvements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implémenter 4 améliorations d'authentification : 2FA mémorisée 30 jours, input OTP en 6 carrés, correction du bug de layout sur ConfirmEmail/Register, et inscription silencieuse si compte existant.

**Architecture:** Changements ciblés dans 5 fichiers existants + 1 nouveau composant Vue. Aucune migration DB. Aucune nouvelle dépendance.

**Tech Stack:** ASP.NET Core 10 (FastEndpoints, xUnit, Moq, Shouldly), Vue 3 (TypeScript, Pinia, vue3-i18n, SCSS)

---

## Fichiers touchés

| Fichier | Action |
|---------|--------|
| `src/Web/appsettings.Development.json` | Modifier — ajouter `TwoFactorAuthenticationDayDelay: 30` |
| `src/Web/appsettings.json` | Modifier — ajouter `TwoFactorAuthenticationDayDelay: 30` |
| `src/Web/vue-app/src/components/forms/OtpInput.vue` | **Créer** — composant OTP 6 carrés |
| `src/Web/vue-app/src/views/TwoFactor.vue` | Modifier — remplacer FormInput par OtpInput |
| `src/Web/vue-app/src/App.vue` | Modifier — ajouter `confirmEmail` et `register` dans `authenticationRoutes` |
| `src/Web/Features/Public/Authentication/Register/RegisterEndPoint.cs` | Modifier — retour silencieux si email existant |
| `tests/Tests.Web/Features/Public/Authentication/Register/RegisterEndpointTests.cs` | **Créer** — tests pour le comportement silencieux |

---

## Task 1 — 2FA : activer le délai de 30 jours

**Fichiers :**
- Modifier : `src/Web/appsettings.Development.json`
- Modifier : `src/Web/appsettings.json`

La logique backend existe déjà dans `AuthenticationService.GetTwoFactorAuthenticationTokenCodeUserWithPassword()` — elle retourne `null` (pas de 2FA) si `LastTwoFactorAuthenticationWasLessThanGivenNumberOfDaysAgo(days)` est vrai. La valeur `0` par défaut rendait ce check toujours faux.

- [ ] **Étape 1 : Ajouter le délai dans `appsettings.Development.json`**

Dans la section `"Application"`, ajouter la clé `TwoFactorAuthenticationDayDelay` :

```json
"Application": {
    "BaseUrl": "https://localhost:7101",
    "RedirectUrl": "https://localhost:44385",
    "ErrorNotificationDestination": "your-email@gmail.com",
    "TwoFactorAuthenticationDayDelay": 30
}
```

- [ ] **Étape 2 : Ajouter le délai dans `appsettings.json`**

Ajouter la section `"Application"` dans `appsettings.json` (base config) :

```json
"Application": {
    "BaseUrl": "",
    "RedirectUrl": "",
    "ErrorNotificationDestination": "",
    "TwoFactorAuthenticationDayDelay": 30
}
```

- [ ] **Étape 3 : Vérifier manuellement**

Se connecter avec un compte 2FA activé → entrer le code → se déconnecter → se reconnecter → vérifier que la 2FA **n'est plus demandée** pendant les 30 prochains jours.

- [ ] **Étape 4 : Commit**

```bash
git add src/Web/appsettings.Development.json src/Web/appsettings.json
git commit -m "config: set 2FA remember delay to 30 days"
```

---

## Task 2 — Créer le composant `OtpInput.vue`

**Fichiers :**
- Créer : `src/Web/vue-app/src/components/forms/OtpInput.vue`

- [ ] **Étape 1 : Créer le fichier**

Créer `src/Web/vue-app/src/components/forms/OtpInput.vue` avec le contenu suivant :

```vue
<template>
  <div class="otp-input">
    <input
      v-for="(_, i) in digits"
      :key="i"
      :ref="(el) => { if (el) inputs[i] = el as HTMLInputElement }"
      :value="digits[i]"
      type="text"
      inputmode="numeric"
      maxlength="1"
      autocomplete="one-time-code"
      class="otp-input__box"
      @input="onInput($event, i)"
      @keydown="onKeydown($event, i)"
      @focus="onFocus($event)"
      @paste.prevent="onPaste($event)"
    />
  </div>
</template>

<script lang="ts" setup>
import { ref, watch } from 'vue'

const props = defineProps<{ modelValue: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const digits = ref<string[]>(Array(6).fill(''))
const inputs = ref<HTMLInputElement[]>([])

watch(
  () => props.modelValue,
  (val) => {
    const clean = val.replace(/\D/g, '').slice(0, 6)
    for (let i = 0; i < 6; i++) {
      digits.value[i] = clean[i] ?? ''
    }
  },
  { immediate: true }
)

function onInput(event: Event, index: number) {
  const char = (event.target as HTMLInputElement).value.replace(/\D/g, '').slice(-1)
  digits.value[index] = char
  emit('update:modelValue', digits.value.join(''))
  if (char && index < 5) inputs.value[index + 1]?.focus()
}

function onKeydown(event: KeyboardEvent, index: number) {
  if (event.key === 'Backspace' && !digits.value[index] && index > 0) {
    inputs.value[index - 1]?.focus()
  }
}

function onFocus(event: FocusEvent) {
  (event.target as HTMLInputElement).select()
}

function onPaste(event: ClipboardEvent) {
  const pasted = (event.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, 6)
  if (pasted.length === 6) {
    digits.value = pasted.split('')
    emit('update:modelValue', pasted)
    inputs.value[5]?.focus()
  }
}
</script>

<style lang="scss" scoped>
@use "@/sass/tools" as *;

.otp-input {
  display: flex;
  gap: 8px;
  justify-content: center;
  margin-bottom: 24px;

  &__box {
    width: 48px;
    height: 48px;
    text-align: center;
    font-size: rem(20);
    font-weight: 600;
    border: 1px solid $color-border;
    border-radius: $common-border-radius;
    padding: 0;
    background-color: $color-white;
    transition: border-color 0.2s cb(snappy), box-shadow 0.2s cb(snappy);

    &:focus {
      outline: none;
      border-color: $color-green;
      box-shadow: 0 0 0 3px rgba($color-green, 0.1);
    }
  }
}
</style>
```

- [ ] **Étape 2 : Commit**

```bash
git add src/Web/vue-app/src/components/forms/OtpInput.vue
git commit -m "feat: add OtpInput component with 6 squares, auto-advance and paste support"
```

---

## Task 3 — Mettre à jour `TwoFactor.vue`

**Fichiers :**
- Modifier : `src/Web/vue-app/src/views/TwoFactor.vue`

- [ ] **Étape 1 : Remplacer le contenu du fichier**

Remplacer **l'intégralité** de `src/Web/vue-app/src/views/TwoFactor.vue` par :

```vue
<template>
    <Card :title="t('routes.twoFactor.name')"
          class="form"
          :is-authentication="true"
          @keyup.enter="sendTwoFactorAuthenticationRequest">
        <Loader v-if="preventMultipleSubmit" />
        <FormTooltip>
            <p v-html="t('pages.twoFactor.tooltip')"></p>
        </FormTooltip>
        <OtpInput v-model="code" />
        <button class="btn btn--full btn--purple btn--big"
                @click="sendTwoFactorAuthenticationRequest"
                :disabled="preventMultipleSubmit">
            {{ t('pages.twoFactor.submit') }}
        </button>
        <TextLink :path="{ path: t('routes.login.path') }"
                  :text="t('pages.twoFactor.loginLink')" />
    </Card>
</template>

<script lang="ts" setup>
import { ref } from "vue"
import { useI18n } from "vue3-i18n"
import { useRouter } from "vue-router"
import { useAuthenticationService, useUserService } from "@/inversify.config"
import { notifyError } from "@/notify"
import { useUserStore } from "@/stores/userStore"
import { useApiStore } from "@/stores/apiStore"
import { ITwoFactorRequest } from "@/types/requests/twoFactorRequest"
import Card from "@/components/layouts/items/Card.vue"
import OtpInput from "@/components/forms/OtpInput.vue"
import FormTooltip from "@/components/layouts/items/Tooltip.vue"
import TextLink from "@/components/layouts/items/TextLink.vue"
import Loader from "@/components/layouts/items/Loader.vue"

const { t } = useI18n()
const router = useRouter()
const apiStore = useApiStore()
const userStore = useUserStore()
const userService = useUserService()
const authenticationService = useAuthenticationService()

const code = ref<string>('')
const preventMultipleSubmit = ref<boolean>(false)

async function sendTwoFactorAuthenticationRequest() {
  if (preventMultipleSubmit.value) return

  if (code.value.length !== 6) {
    notifyError(t('validation.errorsInForm'))
    return
  }

  preventMultipleSubmit.value = true

  const request = { username: userStore.username, code: code.value } as ITwoFactorRequest
  const twoFactorResponse = await authenticationService.twoFactor(request)

  if (!twoFactorResponse.succeeded) {
    const errorMessages = twoFactorResponse.getErrorMessages('pages.twoFactor.validation')
    notifyError(errorMessages.length > 0 ? errorMessages[0] : t('pages.twoFactor.validation.errorOccured'))
    preventMultipleSubmit.value = false
    return
  }

  const user = await userService.getCurrentUser()
  userStore.setUser(user)
  apiStore.setNeedToLogout(false)
  await router.push(t("routes.classes.path"))
  preventMultipleSubmit.value = false
}
</script>
```

- [ ] **Étape 2 : Vérifier visuellement**

Lancer `npm run dev` dans `src/Web/vue-app/`, naviguer sur `/connexion`, se connecter avec un compte 2FA → vérifier que la page `/two-factor` affiche bien 6 carrés, que le focus avance automatiquement, et que coller un code de 6 chiffres remplit tous les carrés.

- [ ] **Étape 3 : Commit**

```bash
git add src/Web/vue-app/src/views/TwoFactor.vue
git commit -m "feat: replace 2FA text input with 6-box OTP input"
```

---

## Task 4 — Corriger le bug de layout sur ConfirmEmail et Register

**Fichiers :**
- Modifier : `src/Web/vue-app/src/App.vue` ligne 18

**Cause :** `authenticationRoutes` ne contient pas `'confirmEmail'` ni `'register'`. Si Pinia a un utilisateur persisté (même avec une session expirée côté backend), `isAuthenticationPath` est `false` sur ces routes → `DashboardLayout` s'affiche au lieu de `AuthenticationLayout`.

- [ ] **Étape 1 : Ajouter les routes manquantes dans `App.vue`**

Remplacer la ligne 18 :

```typescript
// Avant
const authenticationRoutes = ['login', 'twoFactor', 'forgotPassword', 'resetPassword']

// Après
const authenticationRoutes = ['login', 'twoFactor', 'forgotPassword', 'resetPassword', 'confirmEmail', 'register']
```

- [ ] **Étape 2 : Vérifier manuellement**

Être connecté (session Pinia active), puis visiter directement `/confirm-email?userId=xxx&token=yyy` → vérifier que la page affiche bien le layout d'authentification (fond épuré, pas de navbar), et non le dashboard.

Faire de même pour `/inscription`.

- [ ] **Étape 3 : Commit**

```bash
git add src/Web/vue-app/src/App.vue
git commit -m "fix: show auth layout on confirmEmail and register routes regardless of session"
```

---

## Task 5 — Inscription silencieuse si compte existant

**Fichiers :**
- Modifier : `src/Web/Features/Public/Authentication/Register/RegisterEndPoint.cs`
- Créer : `tests/Tests.Web/Features/Public/Authentication/Register/RegisterEndpointTests.cs`

**Comportement cible :** si l'email existe déjà, retourner `succeeded: true` sans envoyer d'email. Le frontend redirige vers `/login` comme si tout s'était bien passé. L'utilisateur ne peut pas deviner si le compte existait.

- [ ] **Étape 1 : Écrire les tests qui échouent**

Créer `tests/Tests.Web/Features/Public/Authentication/Register/RegisterEndpointTests.cs` :

```csharp
using Application.Interfaces.Services.Notifications;
using Application.Interfaces.Services.Users;
using Application.Settings;
using Domain.Entities.Identity;
using Domain.Repositories;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Web.Features.Public.Authentication.Register;

namespace Tests.Web.Features.Public.Authentication.Register;

public class RegisterEndpointTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IMemberRepository> _memberRepository;
    private readonly Mock<INotificationService> _notificationService;
    private readonly Mock<IAuthenticationService> _authenticationService;
    private readonly RegisterEndpoint _endpoint;

    public RegisterEndpointTests()
    {
        _userRepository = new Mock<IUserRepository>();
        _memberRepository = new Mock<IMemberRepository>();
        _notificationService = new Mock<INotificationService>();
        _authenticationService = new Mock<IAuthenticationService>();

        _endpoint = Factory.Create<RegisterEndpoint>(
            _userRepository.Object,
            _memberRepository.Object,
            Mock.Of<ILogger<RegisterEndpoint>>(),
            _notificationService.Object,
            _authenticationService.Object,
            Options.Create(new ApplicationSettings { BaseUrl = "https://localhost:7101" })
        );
    }

    [Fact]
    public async Task WhenHandleAsync_AndEmailAlreadyExists_ThenReturnSucceeded()
    {
        // Arrange
        _authenticationService
            .Setup(x => x.IsTeacherFromPublicCegep(It.IsAny<User>()))
            .Returns(true);
        _userRepository
            .Setup(x => x.UserWithEmailExists(It.IsAny<string>()))
            .Returns(true);

        var request = new RegisterRequest
        {
            Email = "existing@cegepgarneau.ca",
            Password = "Test@1234",
            ConfirmEmailRelativeUrl = "/confirm-email"
        };

        // Act
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Assert
        _endpoint.Response.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task WhenHandleAsync_AndEmailAlreadyExists_ThenNoConfirmationEmailSent()
    {
        // Arrange
        _authenticationService
            .Setup(x => x.IsTeacherFromPublicCegep(It.IsAny<User>()))
            .Returns(true);
        _userRepository
            .Setup(x => x.UserWithEmailExists(It.IsAny<string>()))
            .Returns(true);

        var request = new RegisterRequest
        {
            Email = "existing@cegepgarneau.ca",
            Password = "Test@1234",
            ConfirmEmailRelativeUrl = "/confirm-email"
        };

        // Act
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Assert
        _notificationService.Verify(
            x => x.SendRegisterConfirmationNotification(It.IsAny<User>(), It.IsAny<string>()),
            Times.Never);
    }
}
```

- [ ] **Étape 2 : Lancer les tests pour confirmer qu'ils échouent**

```bash
dotnet test tests/Tests.Web/ --filter "RegisterEndpointTests" -v normal
```

Résultat attendu : **FAIL** (le code actuel retourne `succeeded: false` quand l'email existe).

- [ ] **Étape 3 : Appliquer le fix dans `RegisterEndPoint.cs`**

Remplacer le bloc suivant (lignes 67-74) :

```csharp
// Avant
if (_userRepository.UserWithEmailExists(req.Email))
{
    await Send.OkAsync(
        new SucceededOrNotResponse(false,
            new Error("EmailAlreadyExists", "A user with this email already exists.")
        ), ct);
    return;
}
```

Par :

```csharp
// Après
if (_userRepository.UserWithEmailExists(req.Email))
{
    await Send.OkAsync(new SucceededOrNotResponse(true), ct);
    return;
}
```

- [ ] **Étape 4 : Lancer les tests pour confirmer qu'ils passent**

```bash
dotnet test tests/Tests.Web/ --filter "RegisterEndpointTests" -v normal
```

Résultat attendu : **PASS** sur les 2 tests.

- [ ] **Étape 5 : Lancer tous les tests pour vérifier aucune régression**

```bash
dotnet test --no-build -v minimal
```

Résultat attendu : tous les tests passent.

- [ ] **Étape 6 : Commit**

```bash
git add src/Web/Features/Public/Authentication/Register/RegisterEndPoint.cs
git add tests/Tests.Web/Features/Public/Authentication/Register/RegisterEndpointTests.cs
git commit -m "feat: return success silently when registering with existing email"
```

---

## Vérification finale

- [ ] Se connecter → 2FA demandée → entrer le code → se déconnecter → se reconnecter → **2FA non demandée** (30-day delay actif)
- [ ] Sur la page 2FA, les 6 carrés s'affichent, le focus avance automatiquement, coller fonctionne
- [ ] Visiter `/confirm-email` en étant connecté → **AuthenticationLayout** affiché (pas de navbar)
- [ ] S'inscrire avec un email déjà existant → **redirect vers `/login`**, pas d'erreur affichée, pas d'email envoyé
