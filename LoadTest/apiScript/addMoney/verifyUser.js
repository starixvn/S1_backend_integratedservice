const UatAccounts = require("../UatAccounts.json");
const { getCookie } = require("../utils/getCookie");

const enablePhone = async (custId) => {
  const requestParams = {
    custId: custId,
    status: 1,
  };
  try {
    const response = await fetch(
      `https://boapi.vgamesuat.com/apiV2/Member/SwitchPhoneVerified`,
      {
        method: "POST",
        headers: {
          accept: "application/json, text/plain, */*",
          "cache-control": "no-cache",
          "content-type": "application/json",
          cookie: getCookie(),
        },
        body: JSON.stringify(requestParams),
      }
    );

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(`Error: ${errorData}`);
    }

    return await response.json();
  } catch (error) {
    console.error("Error generating report:", requestParams.custId);
    console.error("respone:", error);
    return null;
  }
};

const enableEmail = async (custId) => {
  const requestParams = {
    custId: custId,
    status: 1,
  };
  try {
    const response = await fetch(
      `https://boapi.vgamesuat.com/apiV2/Member/SwitchEmailVerified`,
      {
        method: "POST",
        headers: {
          accept: "application/json, text/plain, */*",
          "cache-control": "no-cache",
          "content-type": "application/json",
          cookie: getCookie(),
        },
        body: JSON.stringify(requestParams),
      }
    );

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(`Error: ${errorData}`);
    }

    return await response.json();
  } catch (error) {
    console.error("Error generating report:", userdata.userName);
    console.error("respone:", error);
    return null;
  }
};

const updateBirthday = async (custId) => {
  const requestParams = {
    custId: custId,
    birthYear: 1999,
    birthMonth: 12,
    birthDate: 1,
  };
  try {
    const response = await fetch(
      `https://boapi.vgamesuat.com/apiV2/Member/EditBirthday`,
      {
        method: "POST",
        headers: {
          accept: "application/json, text/plain, */*",
          "cache-control": "no-cache",
          "content-type": "application/json",
          cookie: getCookie(),
        },
        body: JSON.stringify(requestParams),
      }
    );

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(`Error: ${errorData}`);
    }

    return await response.json();
  } catch (error) {
    console.error("Error generating report:", userdata.userName);
    console.error("respone:", error);
    return null;
  }
};

(async () => {
    for (let i = 3694; i < UatAccounts.length; i++) {
        await updateBirthday(UatAccounts[i].CustId)
        await enablePhone(UatAccounts[i].CustId)
        await enableEmail(UatAccounts[i].CustId)
        console.log(`${i}: Update user ${UatAccounts[i].UserName} done`)
      }
})()