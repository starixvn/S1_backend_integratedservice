import exec from 'k6/execution';
import { UserInfos } from '../constant/userInfos.js';

export const getTpCustId = (id) => __ENV.RUN_ENV?.toLowerCase() == 'local' ? 'DNova88_' + id : 'uNsDemo_' + id

const UatSabaSportScenarioRangeIds = () => {
  //const userRange = UserInfos.slice(0, 2000)
  const userRange = UserInfos
  return {
    placeBetConfirmBetParlayTest: () => userRange.slice(0, 300),
    placebetConfirmbetSettleTest: () => userRange.slice(301, 1000),
    placebetConfirmbetTest: () => userRange.slice(1001, 1300),
    settleTest: () => userRange.slice(1301, 4000),
    defautTest: () => userRange
  }

  return {
    placeBetConfirmBetParlayTest: () => userRange.slice(0, 150),
    placebetConfirmbetSettleTest: () => userRange.slice(101, 500),
    placebetConfirmbetTest: () => userRange.slice(501, 800),
    settleTest: () => userRange.slice(801, 2000),
    defautTest: () => userRange
  }
}

const LocalSabaSportScenarioRangeIds = () => {
  const userRange = UserInfos.slice(0, 1500);
  
  return {
    placeBetConfirmBetParlayTest: () => userRange.slice(0, 150),
    placebetConfirmbetSettleTest: () => userRange.slice(101, 400),
    placebetConfirmbetTest: () => userRange.slice(401, 700),
    settleTest: () => userRange.slice(701, 1500),
    defautTest: () => userRange
  }
}

const getSabaSportScenarioRangeIds = (scenarioRange) => {
  const env = __ENV.RUN_ENV?.toLowerCase() ?? 'local'
  if (env == 'uat') {
    const userIdRange = UatSabaSportScenarioRangeIds();
    return userIdRange[scenarioRange] ?? userIdRange.defautTest
  }

  const userIdRange = LocalSabaSportScenarioRangeIds();
  return userIdRange[scenarioRange] ?? userIdRange.defautTest
}

export const GetSapaSportUserId = (config = {}) => {
  const { isSmokeTest } = config;
  if (isSmokeTest) {
    return getTpCustId(UserInfos[0].CustId)
  }

  const scenarioRange = getSabaSportScenarioRangeIds(exec.scenario.name.split('-')[0])()
  return getTpCustId(scenarioRange[exec.scenario.iterationInInstance % scenarioRange.length].CustId)
}

const UatSabaPlusScenarioRangeIds = () => {
  const userRange = UserInfos.slice(2001, 4000)

  return {
    placebetEndgameTest: () => userRange.slice(0, 500),
    placebetAndSettleTest: () => userRange.slice(501, 750),
    freeSpinTest: () => userRange.slice(751, 1000),
    playerInfoTest: () => userRange.slice(1001, 1500),
    defautTest: () => userRange
  }
}

const LocalSabaPlusScenarioRangeIds = () => {
  const userRange = UserInfos.slice(1501, 3000);
  
  return {
    placebetEndgameTest: () => userRange.slice(0, 500),
    placebetAndSettleTest: () => userRange.slice(501, 1000),
    freeSpinTest: () => userRange.slice(1001, 1300),
    playerInfoTest: () => userRange.slice(1301, 1500),
    defautTest: () => userRange
  }
}

const getSabaPlusScenarioRangeIds = (scenarioRange) => {
  const env = __ENV.RUN_ENV?.toLowerCase() ?? 'local'
  if (env == 'uat') {
    const userIdRange = UatSabaPlusScenarioRangeIds();
    return userIdRange[scenarioRange] ?? userIdRange.defautTest
  }

  const userIdRange = LocalSabaPlusScenarioRangeIds();
  return userIdRange[scenarioRange] ?? userIdRange.defautTest
}

export const GetSabaPlusCustId = (config = {}) => {
  const { isSmokeTest } = config;
  if (isSmokeTest) {
    return getTpCustId(UserInfos[0].CustId)
  }
  
  const scenarioRange = getSabaPlusScenarioRangeIds(exec.scenario.name.split('-')[0])()
  const userIndex = exec.scenario.iterationInInstance % scenarioRange.length

  const id = getTpCustId(scenarioRange[userIndex].CustId);
  return id;
}

export const GetUser = () => {
  return UserInfos[exec.scenario.iterationInInstance % UserInfos.length]
}