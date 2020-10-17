import React, { Component } from 'react'
import { Input, Grid, Dropdown, Button, Menu, Divider, Icon } from 'semantic-ui-react'
import Avatar from 'avataaars'

import { get, types, rand } from './avatarOptions'

export default class LobbyCreateView extends Component {
    constructor(props) {
        super(props)

        this.state = {
            avatar: this.randomAvatar(),
        }
    }

    handleLobbyJoin = e => {
        const targetType = e.target.nodeName
        if (targetType === 'INPUT') return

        const { lobbyId, joinLobby } = this.props
        if (lobbyId.length === 6) {
            console.log('Trying to join Lobby: ', lobbyId)
            joinLobby()
        }
    }

    handleAvatarOptionChange = (e, props) => {
        const { optiontype, value } = props
        const avatar = this.state.avatar
        avatar[optiontype] = value
        this.setState({ avatar }, () => {
            const { updateAvatar } = this.props
            updateAvatar(this.state.avatar)
        })
    }

    randomAvatar = () => {
        const avatar = {
            topType: rand('topType'),
            accessoriesType: rand('accessoriesType'),
            hatColor: rand('hatColor'),
            facialHairType: rand('facialHairType'),
            facialHairColor: rand('facialHairColor'),
            clotheType: rand('clotheType'),
            eyeType: rand('eyeType'),
            skinColor: rand('skinColor'),
        }
        this.props.updateAvatar(avatar)
        return avatar
    }

    render() {
        const { lobbyId, createLobby, joinLobby, handleInputChange, updateAvatar } = this.props

        const dropdowns = Object.keys(types).map(type => (
            <Dropdown
                key={type}
                value={this.state.avatar[type]}
                optiontype={type}
                options={get(type)}
                onChange={this.handleAvatarOptionChange}
                selection
                size={'mini'}
                style={{ width: '50%', display: 'inline-block', background: 'rgba(0,0,0,0.04)', borderRadius: 0, border: 'none', fontSize: '90%' }}
            />
        ))

        return (
            <Grid columns={3} divided id={'gameView'}>
                <Grid.Column
                    style={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        padding: '5%',
                    }}
                    id={'createColumn'}
                    onClick={createLobby}
                >
                    <div style={{ height: 'auto' }}>
                        <h1>Spiel erstellen</h1>
                        <p>Erstelle ein Spiel dem deine Freunde beitreten können</p>
                        <h1 className='clickToAction'>Klicken zum Erstellen</h1>
                    </div>
                </Grid.Column>
                <Grid.Column style={{ textAlign: 'center' }}>
                    <Avatar
                        avatarStyle='Transparent'
                        mouthType='Default'
                        eyebrowType='Default'
                        topType={this.state.avatar['topType']}
                        accessoriesType={this.state.avatar['accessoriesType']}
                        hatColor={this.state.avatar['hatColor']}
                        facialHairType={this.state.avatar['facialHairType']}
                        facialHairColor={this.state.avatar['facialHairColor']}
                        clotheType={this.state.avatar['clotheType']}
                        eyeType={this.state.avatar['eyeType']}
                        skinColor={this.state.avatar['skinColor']}
                        style={{ marginBottom: '-1.4rem' }}
                    />
                    <Divider />
                    <Input name={'name'} onChange={this.props.handleInputChange} fluid transparent value={this.props.name} size={'huge'} className='nameInput'>
                        <input />
                        <Icon name='pencil' />
                    </Input>
                    <div>{dropdowns}</div>
                    <Button
                        icon={'random'}
                        circular
                        onClick={() => this.setState({ avatar: this.randomAvatar() })}
                        style={{ marginTop: '2rem', marginBottom: '1rem' }}
                    />
                    <Divider />
                    <Menu pointing secondary style={{ marginTop: '0.9%', border: 'none' }}>
                        <Menu.Item name='Anmelden' onClick={this.login} />
                        <Menu.Item name='Registrieren' disabled />
                    </Menu>
                </Grid.Column>
                <Grid.Column
                    style={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        padding: '5%',
                    }}
                    id={'joinColumn'}
                    onClick={this.handleLobbyJoin}
                >
                    <div style={{ height: 'auto' }}>
                        <h1>Spiel Beitreten</h1>
                        <p>
                            Wenn ein Freund ein Spiel erstellt kannst du hier den Code eingeben und dem Spiel beitreten. <b>Viel Spaß!</b>
                        </p>
                        <Input
                            name='lobbyId'
                            value={lobbyId}
                            placeholder='Code'
                            onChange={(e, p) => handleInputChange(e, p, 6)}
                            className={'inputUppercase'}
                            style={{
                                textAlign: 'center',
                                width: '190px',
                                fontSize: '25px',
                            }}
                        />
                        <h1 className='clickToAction'>Klicken zum Beitreten</h1>
                    </div>
                </Grid.Column>
            </Grid>
        )
    }
}
