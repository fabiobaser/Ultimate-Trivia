import React, { Component } from 'react'
import { Input, Image, Modal, Menu } from 'semantic-ui-react'
import { HubConnectionBuilder } from '@microsoft/signalr'
import faker from 'faker'
import LobbyCreateView from './GameViews/LobbyCreateView'
import './App.scss'
import Config from './config'
import { randomAvatar } from './GameViews/avatarOptions'
import GameView from './Components/GameView'
import Granim from 'react-granim'

import store from './redux/store'
import { signinRedirect } from './Components/api-authentication/services/userService'
import { fetchRegisteredUser } from './Components/api-authentication/services/apiService'
import { loadUserFromStorage } from './Components/api-authentication/services/userService'

export default class App extends Component {
    constructor(props) {
        super(props)

        this.state = {
            name: faker.name.firstName(),
            lobbyId: '',
            userArray: [],
            connectedToLobby: false,
            showSelectableQuestions: false,
            selectableQuestions: [],
            possibleAnswers: [],
            nameModalOpen: false,
            chat: [],
            inGame: false,
            gameState: 'initial', //"initial", "lobby"
            topics: [],
            question: '',
            points: {},
            results: [],
            avatar: randomAvatar(),
        }
    }

    async componentDidMount() {
        let user = await fetchRegisteredUser()
        console.debug('%coidc User: ', 'color: orange', user)
        if (user) {
            this.setState({
                name: user.name ?? this.state.name,
                avatar: user.avatarJson ? JSON.parse(user.avatarJson) : this.state.avatar,
            })
        }

        await this.connectToHub()
        window.rapp = this
    }

    sendMessage = message => {
        this.connection.invoke('SendMessage', message)
    }

    createGame = () => {
        this.connection.invoke('StartGame', { rounds: 3, answerDuration: 3000 })
    }

    createLobby = () => {
        this.connection
            .invoke('CreateLobby', {
                name: this.state.name,
                avatarJson: JSON.stringify(this.state.avatar),
            })
            .catch(console.error)
    }

    joinLobby = () => {
        const { name, lobbyId } = this.state
        console.log('%cConnecting to code: ', 'color: orange', lobbyId)
        this.connection.invoke('JoinLobby', { name, avatarJson: JSON.stringify(this.state.avatar) }, lobbyId)
    }

    leaveLobby = () => {
        this.connection.invoke('LeaveLobby')
        this.setState({ chat: [], gameState: 'initial' })
    }

    pushtToChat = ({ name = '', avatarJson = '', system = true }, message) => {
        const newEntry = { sender: name, avatar: JSON.parse(avatarJson), system, message }
        const chat = this.state.chat
        chat.push(newEntry)

        this.setState({ chat: chat })
    }

    copyLobbyId = (lobbyId = this.state.lobbyId) => {
        const copyToClipboard = str => {
            const el = document.createElement('textarea')
            el.value = str
            document.body.appendChild(el)
            el.select()
            document.execCommand('copy')
            document.body.removeChild(el)
        }

        copyToClipboard(lobbyId)
    }

    connectToHub = async () => {
        let user = await loadUserFromStorage(store)

        this.connection = new HubConnectionBuilder()
            .withUrl(Config.baseURL + '/triviaGameServer', { accessTokenFactory: () => user.access_token })
            .build()
        window.connection = this.connection

        this.connection.on('broadcastMessage', ({ name, avatarJson }, message) => {
            console.debug('broadcastMessage: ', name, message)
            this.pushtToChat({ name, avatarJson, system: false }, message)
        })

        this.connection.on('joinlobby', joinLobbyEvent => {
            console.debug('%cjoinLobbyEvent: ', 'color: orange', joinLobbyEvent)

            const { player, players, creatorId, lobbyId } = joinLobbyEvent

            //this.copyLobbyId(joinLobbyEvent.lobbyId);

            this.setState({
                userArray: players,
                lobbyCreator: creatorId,
                connectedToLobby: true,
                playerId: player.id,
                lobbyId: lobbyId,
                inGame: false,
                gameState: 'lobby',
            })
        })

        this.connection.on('userJoinedLobby', userJoinedEvent => {
            console.debug('%cuserJoinedEvent: ', 'color: orange', userJoinedEvent)

            const { newPlayer, players } = userJoinedEvent
            const { name, avatarJson } = newPlayer

            this.pushtToChat({ name, avatarJson }, `${name} ist dem Spiel beigetreten`)

            this.setState({ userArray: players })
        })

        this.connection.on('leaveLobby', () => {
            console.log(`You have left the lobby`)
            this.setState({ connectedToLobby: false, userArray: [] })
        })

        this.connection.on('userLeftLobby', userLeftEvent => {
            console.log(`${userLeftEvent.leavingUser} has left the lobby`)
            console.log('In the lobby are: ', userLeftEvent.usernames)

            this.setState({ userArray: userLeftEvent.usernames })
        })

        this.connection.on('gameStarted', gameStartedEvent => {
            console.debug('%cgameStartedEvent: ', 'color: orange', gameStartedEvent)

            const { points, currentQuestionNr, currentRoundNr, maxQuestionNr, maxRoundNr } = gameStartedEvent

            this.setState({ inGame: true, points, currentQuestionNr, currentRoundNr, maxQuestionNr, maxRoundNr })
        })

        this.connection.on('nextRoundStarted', nextRoundStartedEvent => {})

        this.connection.on('categorySelected', categorySelectedEvent => {
            console.debug('%ccategorySelectedEvent: ', 'color: orange', categorySelectedEvent)

            const { player, category } = categorySelectedEvent
            const { name, avatarJson } = player

            this.pushtToChat({ name, avatarJson, system: true }, `${name} hat ${category.content} gewählt`)
        })

        this.connection.on('showCategories', showCategoriesEvent => {
            console.debug('%cshowCategoriesEvent: ', 'color: orange', showCategoriesEvent)

            const { categories, currentPlayer } = showCategoriesEvent

            this.pushtToChat(
                { name: currentPlayer.name, avatarJson: currentPlayer.avatarJson, system: true },
                `${currentPlayer.name} wählt eine Kategorie aus`
            )

            if (currentPlayer.name === this.state.name) {
                this.setState({
                    topics: categories,
                    gameState: 'topicSelect',
                })
            } else {
                this.setState({ question: '', possibleAnswers: [], topics: [], gameState: 'lobby' })
            }
        })

        this.connection.on('showQuestion', showQuestionEvent => {
            console.debug('%cshowQuestionEvent: ', 'color: orange', showQuestionEvent)

            const {
                answers,
                question,
                currentQuestionNr,
                currentRoundNr,
                maxQuestionNr,
                maxRoundNr,
            } = showQuestionEvent

            this.setState({
                possibleAnswers: answers,
                question,
                currentQuestionNr,
                maxRoundNr,
                maxQuestionNr,
                currentRoundNr,
                gameState: 'question',
            })
        })

        // TODO: handle correctly
        this.connection.on('playerAnswered', userAnsweredEvent => {
            console.debug('%cuserAnsweredEvent: ', 'color: orange', userAnsweredEvent)
        })

        // TODO: handle correctly
        this.connection.on('highlightCorrectAnswer', highlightCorrectAnswerEvent => {
            console.debug('%chighlightCorrectAnswer: ', 'color: orange', highlightCorrectAnswerEvent)

            const { answers } = highlightCorrectAnswerEvent

            this.setState({
                gameState: 'questionsResult',
                results: answers,
            })
        })

        this.connection.on('updatePoints', updatePointsEvent => {
            this.setState({ points: updatePointsEvent.points })
        })

        this.connection.on('showFinalResult', showFinalResultEvent => {
            console.log('Final result: ', showFinalResultEvent.points)
            this.pushtToChat('', 'Spiel zuende')
        })

        // TODO: handle correctly
        this.connection.on('error', errorEvent => {
            if (errorEvent.errorCode === 'DUPLICATE_USERNAME') {
                console.error('username already taken')
                console.error(errorEvent.errorMessage)
            } else {
                console.error(errorEvent.errorCode + ' ' + errorEvent.errorMessage)
            }
        })

        this.connection.start().catch(error => {
            console.error(error)
        })
    }

    handleInputChange = (e, props, maxLength = 100) => {
        const { name, value } = props
        if (this.state.connectedToLobby) return
        if (value.length > maxLength) return
        const newState = this.state
        newState[name] = value
        this.setState(newState)
    }

    handleAnswerSelect = answer => {
        this.connection.invoke('answerSelected', answer)
        //this.setState({ possibleAnswers: [] });
    }

    handleTopicSelect = topic => {
        this.connection.invoke('categorySelected', topic)
        this.setState({ topics: [] })
    }

    closeModal = () => {
        this.setState({ nameModalOpen: false })
    }

    updateAvatar = avatar => {
        this.setState({ avatar })
    }

    // AUTHENTICATION TEST PLAYGROUND

    login = () => {
        signinRedirect().catch(e => {
            console.log(e)
        })
    }

    render() {
        const {
            lobbyId,
            name,
            chat,
            userArray,
            gameState,
            topics,
            question,
            possibleAnswers,
            results,
            lobbyCreator,
            playerId,
            points,
            avatar,
            currentQuestionNr,
            maxQuestionNr,
            currentRoundNr,
            maxRoundNr,
        } = this.state

        return (
            <div id={'appContainer'} style={{ display: 'flex', flexDirection: 'column' }}>
                <Granim id='gradientBackgound'></Granim>
                <div id={'backdropView'} style={{ flex: 1, background: 'rgba(229, 233, 236, 1.00)' }}>
                    {this.state.gameState === 'initial' && (
                        <LobbyCreateView
                            avatar={avatar}
                            lobbyId={this.state.lobbyId}
                            createLobby={this.createLobby}
                            joinLobby={this.joinLobby}
                            login={this.login}
                            name={this.state.name}
                            handleInputChange={this.handleInputChange}
                            updateAvatar={this.updateAvatar}
                        />
                    )}

                    {['lobby', 'topicSelect', 'question', 'questionsResult'].includes(gameState) && (
                        <GameView
                            avatar={this.state.avatar}
                            chat={chat}
                            copyLobbyId={this.copyLobbyId}
                            createGame={this.createGame}
                            gameState={gameState}
                            handleAnswerSelect={this.handleAnswerSelect}
                            handleTopicSelect={this.handleTopicSelect}
                            leaveLobby={this.leaveLobby}
                            lobbyCreator={lobbyCreator}
                            lobbyId={lobbyId}
                            name={name}
                            playerId={playerId}
                            points={points}
                            possibleAnswers={possibleAnswers}
                            question={question}
                            results={results}
                            sendMessage={this.sendMessage}
                            topics={topics}
                            userArray={userArray}
                            currentQuestionNr={currentQuestionNr}
                            maxQuestionNr={maxQuestionNr}
                            currentRoundNr={currentRoundNr}
                            maxRoundNr={maxRoundNr}
                        />
                    )}
                </div>
            </div>
        )
    }
}
