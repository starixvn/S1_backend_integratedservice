import { GameOptions } from "../constant/GameOptions.js";


export const GetGameOption = () => GameOptions[Math.floor(Math.random()*GameOptions.length)]
