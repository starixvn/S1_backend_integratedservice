import exec from 'k6/execution';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';
import { UserInfos } from '../constant/userInfos.js';

export const getTpCustId = (id) => __ENV.RUN_ENV?.toLowerCase() == 'local' ? 'DNova88_' + id : 'uNsDemo_' + id

const SabaSportUserInfos = __ENV.RUN_ENV?.toLowerCase() == 'local' ? UserInfos.slice(0, 2000) : UserInfos.slice(2001, 4000);
const getSabaSportScenarioRangeIds = (scenarioRange) => {
  switch (true){
    case scenarioRange == 'placeBetConfirmBetParlayTest':
      return SabaSportUserInfos.slice(0, 100)
    case scenarioRange == 'placebetConfirmbetSettleTest':
      return SabaSportUserInfos.slice(101, 500)
    case scenarioRange == 'placebetConfirmbetTest':
      return SabaSportUserInfos.slice(501, 600)

    case scenarioRange == 'settleTest':
        return SabaSportUserInfos.slice(601, 2000)
    default:
       return UserInfos
  }
}

export const GetSapaSportUserId = () => {
  const scenarioRange = getSabaSportScenarioRangeIds(exec.scenario.name.split('-')[0])
  return getTpCustId(scenarioRange[exec.scenario.iterationInInstance % scenarioRange.length].CustId)
}

// TODO: Remove when run SabaMixed
const SabaPlusUserInfos = UserInfos.slice(0, 4000);
const RapidSabaPlusUserInfos = UserInfos.slice(2801, 3000);
const getSabaPlusScenarioRangeIds = (scenarioRange, isRapidUserId) => {
  if (isRapidUserId) {
    return RapidSabaPlusUserInfos
  }

  switch (true){
    case scenarioRange == 'placebetEndgameTest':
        return SabaPlusUserInfos.slice(0, 1000)
    case scenarioRange == 'placebetAndSettleTest':
      return SabaPlusUserInfos.slice(1001, 2000)
    case scenarioRange == 'freeSpinTest':
        return SabaPlusUserInfos.slice(2001, 3000)
    case scenarioRange == 'playerInfoTest' || scenarioRange == 'sabaplusgetBalanceTest':
        return SabaPlusUserInfos.slice(3001, 4000)
    default:
       return SabaPlusUserInfos
  }
}

// TODO: Use when run SabaMixed
// const SabaPlusUserInfos = UserInfos.slice(2001, 4000);
// const getSabaPlusScenarioRangeIds = (scenarioRange, isRapidUserId) => {

//   switch (true){
//     case scenarioRange == 'placebetEndgameTest':
//         return SabaPlusUserInfos.slice(0, 1000)
//     case scenarioRange == 'placebetAndSettleTest':
//       return SabaPlusUserInfos.slice(1001, 1400)
//     case scenarioRange == 'freeSpinTest':
//         return SabaPlusUserInfos.slice(1401, 1800)
//     case scenarioRange == 'playerInfoTest' || scenarioRange == 'sabaplusgetBalanceTest':
//         return SabaPlusUserInfos.slice(1801, 2000)
//     default:
//        return SabaPlusUserInfos
//   }
// }

export const GetSabaPlusCustId = () => {
  const isRapidUserId = __ENV.RAPID_USER_ID ?? false;
  const scenarioRange = isRapidUserId ? RapidSabaPlusUserInfos : getSabaPlusScenarioRangeIds(exec.scenario.name.split('-')[0], isRapidUserId)
  const userIndex = isRapidUserId ? randomIntBetween(0, scenarioRange.length - 1) : exec.scenario.iterationInInstance % scenarioRange.length

  const id = getTpCustId(scenarioRange[userIndex].CustId);
  return id;
}

export const GetLoginUser = () => {
  return RapidSabaPlusUserInfos[exec.scenario.iterationInInstance % RapidSabaPlusUserInfos.length]
}

export const GetUser = () => {
  return UserInfos[exec.scenario.iterationInInstance % UserInfos.length]
}