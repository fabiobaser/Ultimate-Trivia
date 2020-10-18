import { UserManager } from 'oidc-client';
import { storeUserError, storeUser } from '../actions/authActions'

const config = {
  authority: "https://localhost:5001",
  client_id: "ultimate-trivia-client",
  redirect_uri: "https://localhost:1234/signin-oidc",
  response_type: "code",
  scope: "openid profile UltimateTriviaAPI",
  post_logout_redirect_uri: "https://localhost:1234/signout-oidc",
};


// const config = {
//   authority: "https://quiz.fabiobaser.de:5001",
//   client_id: "ultimate-trivia-client",
//   redirect_uri: "https://quiz.fabiobaser.de:1234/signin-oidc",
//   response_type: "code",
//   scope: "openid profile UltimateTriviaAPI",
//   post_logout_redirect_uri: "https://quiz.fabiobaser.de:1234/signout-oidc",
// };

const userManager = new UserManager(config)

export async function loadUserFromStorage(store) {
  try {
    let user = await userManager.getUser()
    if (!user) { return store.dispatch(storeUserError()) }
    store.dispatch(storeUser(user))
    return user;
  } catch (e) {
    console.error(`User not found: ${e}`)
    store.dispatch(storeUserError())
  }
}

export function signinRedirect() {
  return userManager.signinRedirect()
}

export function signinRedirectCallback() {
  return userManager.signinRedirectCallback()
}

export function signoutRedirect() {
  userManager.clearStaleState()
  userManager.removeUser()
  return userManager.signoutRedirect()
}

export function signoutRedirectCallback() {
  userManager.clearStaleState()
  userManager.removeUser()
  return userManager.signoutRedirectCallback()
}

export default userManager