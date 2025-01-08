const fs = require("fs");
const UatAccounts = require("../UatAccounts.json");

const jsonString = JSON.stringify(UatAccounts, null, 2);
const content = `export const UatUserInfo = ${jsonString}`
// Write JSON string to a file
fs.writeFile("userInfos.js", content, "utf8", (err) => {
  if (err) {
    console.error("Error writing to file", err);
  } else {
    console.log("Array written to file successfully");
  }
});
