import React, {Component} from 'react';
import PropTypes from 'prop-types';
import {Button} from "semantic-ui-react";

export default class SelectButton extends Component {
    render() {
        const {value, handler } = this.props;

        return (
            <Button key={value} onClick={() => handler(value)}>{value}</Button>
        );
    }
}