import React, { useEffect } from 'react'
import { signinRedirectCallback } from '../Components/api-authentication/services/userService'
import { useHistory } from 'react-router-dom'
import { loginCallback } from '../Components/api-authentication/services/apiService'

function SigninOidc() {
  const history = useHistory()
  useEffect(() => {
    async function signinAsync() {
      
      await signinRedirectCallback()
      await loginCallback()

      history.push('/')
    }
    signinAsync()
  }, [history])

  return (
    <div>
      Redirecting...
    </div>
  )
}

export default SigninOidc
