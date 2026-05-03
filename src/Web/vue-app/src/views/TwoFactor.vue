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
