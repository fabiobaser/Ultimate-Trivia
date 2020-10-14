import React from 'react'
import { Route, Redirect } from 'react-router-dom'
import { useSelector } from 'react-redux'

function AuthorizedRoute({ children, component: Component, ...rest }) {
  const user = useSelector(state => state.auth.user)

  return user
    ? (<Route {...rest} component={Component} />)
    : (<Redirect to={'/login'} />)
}

export default AuthorizedRoute