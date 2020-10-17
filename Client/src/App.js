import React, { Component } from 'react'
import { Input, Image, Modal, Menu } from 'semantic-ui-react'
import { HubConnectionBuilder } from '@microsoft/signalr'
import faker from 'faker'
import LobbyCreateView from './GameViews/LobbyCreateView'
import './App.scss'
import Config from './config'

import GameView from './Components/GameView'

import { signinRedirect } from './Components/api-authentication/services/userService'
import { loadUserFromStorage } from './Components/api-authentication/services/userService'

import store from './redux/store'

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
        }        
    }

    componentDidMount() {
        this.connectToHub()
        window.rapp = this
    }

    sendMessage = message => {
        this.connection.invoke('SendMessage', message)
    }

    createGame = () => {
        this.connection.invoke('StartGame', { rounds: 3, answerDuration: 30 })
    }

    createLobby = () => {
        this.connection.invoke('CreateLobby', {
            name: this.state.name,
            avatarJson: JSON.stringify(this.state.avatar),
        })
    }

    joinLobby = () => {
        const { name, lobbyId } = this.state
        console.log('%cConnecting to code: ', 'color: orange', lobbyId)
        this.connection.invoke('JoinLobby', { name, avatarJson: JSON.stringify(this.state.avatar) }, lobbyId)
    }

    leaveLobby = () => {
        this.pushtToChat('', 'You left the lobby')
        this.connection.invoke('LeaveLobby')
        this.setState({ chat: [], gameState: 'initial' })
    }

    pushtToChat = ({ name, avatarJson }, message) => {
        const newEntry = { sender: name, avatar: JSON.parse(avatarJson), message }
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

    connectToHub = () => {
        this.connection = new HubConnectionBuilder().withUrl(Config.baseURL + '/triviaGameServer').build()
        window.connection = this.connection

        this.connection.on('broadcastMessage', ({ name, avatarJson }, message) => {
            console.debug('broadcastMessage: ', name, message)
            this.pushtToChat({ name, avatarJson }, message)
        })

        this.connection.on('joinlobby', joinLobbyEvent => {
            this.pushtToChat('', 'Du bist dem Spiel beigetreten')

            console.log('%cjoinLobbyEvent: ', 'color: blue', joinLobbyEvent)

            //this.copyLobbyId(joinLobbyEvent.lobbyId);

            this.setState({
                userArray: joinLobbyEvent.players,
                lobbyCreator: joinLobbyEvent.creatorId,
                connectedToLobby: true,
                lobbyId: joinLobbyEvent.lobbyId,
                inGame: false,
                gameState: 'lobby',
            })
        })

        this.connection.on('userjoinedlobby', userJoinedEvent => {
            console.log('%cuserJoinedEvent: ', 'color: blue', userJoinedEvent)

            this.pushtToChat('', `${userJoinedEvent.newUser} ist dem Spiel beigetreten`)

            this.setState({ userArray: userJoinedEvent.players })
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

        this.connection.on('gameStarted', () => {
            this.setState({ inGame: true })
        })

        this.connection.on('nextRoundStarted', nextRoundStartedEvent => {})

        this.connection.on('categorySelected', ({ username, category }) => {
            this.pushtToChat('', `${username} hat "${category}" ausgewählt`)
        })

        this.connection.on('showCategories', showCategoriesEvent => {
            console.log(
                `user ${showCategoriesEvent.username} is choosing a category, ${showCategoriesEvent.categories}`
            )

            this.pushtToChat('', `${showCategoriesEvent.username} wählt ein Thema aus`)

            if (showCategoriesEvent.username === this.state.name) {
                this.setState({
                    topics: showCategoriesEvent.categories,
                    gameState: 'topicSelect',
                })
            }

            this.setState({ question: '', possibleAnswers: [] })
        })

        this.connection.on('showQuestion', showQuestionEvent => {
            console.log('Question', showQuestionEvent)
            this.setState({
                possibleAnswers: showQuestionEvent.answers,
                question: showQuestionEvent.question,
                gameState: 'question',
            })
        })

        // TODO: handle correctly
        this.connection.on('playerAnswered', userAnsweredEvent => {
            console.log(`${userAnsweredEvent.username} has answered`)
        })

        // TODO: handle correctly
        this.connection.on('highlightCorrectAnswer', highlightCorrectAnswerEvent => {
            this.setState({
                gameState: 'questionsResult',
                results: highlightCorrectAnswerEvent.answers,
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

        this.connection
            .start()
            .then(result => {
                console.log(result)
            })
            .catch(error => {
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
        this.connection.invoke('AnswerSelected', answer)
        //this.setState({ possibleAnswers: [] });
    }

    handleTopicSelect = topic => {
        this.connection.invoke('CategorySelected', topic)
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
        signinRedirect()
    }

    callApi = () => {
        loadUserFromStorage(store).then(user => {
            console.log(user)

            fetch('https://quiz.fabiobaser.de:5001/debug', {
                headers: {
                    Authorization: 'Bearer ' + user.access_token,
                },
            })
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
            points,
        } = this.state

        return (
            <div id={'appContainer'} style={{ display: 'flex', flexDirection: 'column' }}>
                {/*<button onClick={() => this.login()}>Login</button>
                <button onClick={() => this.callApi()}>Call api</button>*/}
                <div id={'backdropView'} style={{ flex: 1, background: 'rgba(229, 233, 236, 1.00)' }}>
                    {this.state.gameState === 'initial' && (
                        <LobbyCreateView
                            lobbyId={this.state.lobbyId}
                            createLobby={this.createLobby}
                            joinLobby={this.joinLobby}
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
                            points={points}
                            possibleAnswers={possibleAnswers}
                            question={question}
                            results={results}
                            sendMessage={this.sendMessage}
                            topics={topics}
                            userArray={userArray}
                        />
                    )}
                </div>
            </div>
        )
    }
}
