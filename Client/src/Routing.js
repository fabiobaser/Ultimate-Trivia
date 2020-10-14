import React, { Component, useEffect } from 'react';
import { BrowserRouter as Router, Route, Switch } from 'react-router-dom'
import SigninOidc from './pages/signin-oidc'
import SignoutOidc from './pages/signout-oidc'
import { Provider } from 'react-redux';
import store from './redux/store';
import userManager, { loadUserFromStorage } from './Components/api-authentication/services/userService'
import AuthProvider from './Components/api-authentication/authProvider'
import App from "./App";

export default class Routing extends Component {
    constructor(props) {
        super(props);

    }

    render() {
        return (
            <Provider store={store}>
                <AuthProvider userManager={userManager} store={store}>
                    <Router>
                        <Switch>
                            <Route exact path="/" component={App} />
                            <Route path="/signout-oidc" component={SignoutOidc} />
                            <Route path="/signin-oidc" component={SigninOidc} />
                        </Switch>
                    </Router>
                </AuthProvider>
            </Provider>
        );
    }
}