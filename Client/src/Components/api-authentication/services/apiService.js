import { loadUserFromStorage } from "./userService";
import store from "../../../redux/store";
import Config from "../../../config"

async function loginCallback() {

  let user = await loadUserFromStorage(store)

  await fetch(Config.baseURL + '/api/v1/account/login-callback', {
    headers: {
      Authorization: 'Bearer ' + user.access_token,
    }
  });
}

async function fetchRegisteredUser() {

  let user = await loadUserFromStorage(store)

  let response = await fetch(Config.baseURL + '/api/v1/account/user', {
    headers: {
      Authorization: 'Bearer ' + user.access_token,
    }
  });
  if (response.ok) {
    return response.json()
  }
  
}

export {
  loginCallback,
  fetchRegisteredUser
}
