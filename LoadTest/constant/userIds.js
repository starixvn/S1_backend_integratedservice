import { LocalUserIds } from "./userIds/LocalUserIds.js"


const userIdsWithEnvironment = {
  local: LocalUserIds
}
export const UserIds = userIdsWithEnvironment[__ENV.RUN_ENV?.toLowerCase() ?? 'local']
