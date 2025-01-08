import { LocalUserInfos } from "./userIds/LocalUserInfos.js"
import { UatUserInfo } from "./userIds/UatUserInfos.js"

const userInfosWithEnvironment = {
  local: LocalUserInfos,
  uat: UatUserInfo
}

export const UserInfos = userInfosWithEnvironment[__ENV.RUN_ENV?.toLowerCase() ?? 'local'] ?? userInfosWithEnvironment.local
