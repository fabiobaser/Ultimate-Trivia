import React, { Component } from 'react'
import { Grid, Input, Ref, Feed, Label, List, Button, Icon, Popup } from 'semantic-ui-react'
import _ from 'lodash'
import Avatar from 'avataaars'

import '../App.scss'

const abc = 'ABCD'

export default class GameView extends Component {
    state = {
        message: '',
        selectedItemIndex: -1,
        selectedItem: '',
    }

    handleChatSend = () => {
        const { message } = this.state
        const { sendMessage } = this.props
        sendMessage(message)
        this.setState({ message: '' })
    }

    handleSelect = (entryIndex, entry) => {
        const { gameState } = this.props
        if (gameState === 'questionsResult') return
        this.setState({ selectedItemIndex: entryIndex, selectedItem: entry })
    }

    handleSubmit = () => {
        const { handleTopicSelect, handleAnswerSelect, gameState } = this.props
        const { selectedItem } = this.state

        if (selectedItem === '') return

        switch (gameState) {
            case 'topicSelect':
                handleTopicSelect(selectedItem)
                break

            case 'question':
                handleAnswerSelect(selectedItem)
                break
        }

        this.setState({ selectedItemIndex: -1, selectedItem: '' })
    }

    scrollChat = ref => {
        if (!ref) return
        ref.scrollTop = ref.scrollHeight
    }

    render() {
        const { message } = this.state
        const {
            chat,
            userArray,
            lobbyId,
            copyLobbyId,
            leaveLobby,
            createGame,
            gameState,
            topics,
            points,
            question,
            lobbyCreator,
            playerId,
            possibleAnswers,
            results,
        } = this.props

        let questionText = ''
        let selectionArray = []

        switch (gameState) {
            case 'topicSelect':
                selectionArray = topics.map(a => ({ content: a }))
                questionText = 'Bitte wähle eine Kategorie für die nächste Runde aus'
                break

            case 'questionsResult':
                questionText = question
                selectionArray = results
                break

            case 'question':
                questionText = question
                selectionArray = possibleAnswers.map(a => ({ content: a }))
                break
        }

        let userPointsArray = userArray.map(user => ({
            username: user.name,
            points: points[user.name] || 0,
            avatar: JSON.parse(user.avatarJson),
        }))

        userPointsArray = _.sortBy(userPointsArray, u => u.points).reverse()

        return (
            <Grid columns={3} divided id={'gameView'}>
                <Grid.Column width={4} style={{ display: 'flex', flexDirection: 'column', padding: '2rem' }}>
                    <div style={{ display: 'flex' }}>
                        <h1 style={{ flex: 1 }}>Spieler</h1>
                        <Button.Group style={{ display: 'block' }}>
                            <Popup
                                position={'bottom center'}
                                content={'Spiel verlassen'}
                                trigger={
                                    <Button icon basic color='red' onClick={leaveLobby} className='noBorderBoxShadow'>
                                        <Icon name='sign out' />
                                    </Button>
                                }
                            />
                            <Popup
                                position={'bottom center'}
                                content={lobbyId + ' kopieren'}
                                trigger={
                                    <Button
                                        icon
                                        basic
                                        color='grey'
                                        onClick={() => copyLobbyId()}
                                        className='noBorderBoxShadow'
                                    >
                                        <Icon name='clipboard' />
                                    </Button>
                                }
                            />
                        </Button.Group>
                    </div>
                    <List ordered style={{ flex: 1 }}>
                        {userPointsArray.map((userObj, userIndex) => {
                            return (
                                <List.Item key={userObj.username}>
                                    <Avatar
                                        style={{ width: '40px', height: '40px' }}
                                        avatarStyle='Circle'
                                        eyebrowType='Default'
                                        mouthType='Default'
                                        {...userObj.avatar}
                                    />

                                    <List.Content>
                                        <List.Header>{userObj.username}</List.Header>
                                        {(userObj.points || 0) * 10} Punkte
                                    </List.Content>
                                </List.Item>
                            )
                        })}
                    </List>

                    {gameState === 'lobby' && lobbyCreator === playerId && (
                        <Button fluid color={'green'} content='Starten' onClick={createGame} />
                    )}
                </Grid.Column>
                <Grid.Column width={4} style={{ display: 'flex', flexDirection: 'column', padding: '2rem' }}>
                    <h1>Chat</h1>
                    <Ref innerRef={ref => this.scrollChat(ref)}>
                        <Feed style={{ marginTop: '1rem', flex: 1 }}>
                            {chat.map((entry, index) => {
                                const { sender, avatar, system, message } = entry
                                return (
                                    <Feed.Event className={sender === '' ? 'systemMessage' : ''} key={sender + index}>
                                        <Feed.Label
                                            image={
                                                system ? null : (
                                                    <Avatar
                                                        style={{ width: '40px', height: '40px' }}
                                                        avatarStyle='Circle'
                                                        eyebrowType='Default'
                                                        mouthType='Default'
                                                        {...avatar}
                                                    />
                                                )
                                            }
                                        />
                                        <Feed.Content date={system ? '' : sender} summary={message} />
                                    </Feed.Event>
                                )
                            })}
                        </Feed>
                    </Ref>
                    <Input
                        fluid
                        name={'message'}
                        value={message}
                        onKeyDown={e => e.which === 13 && this.handleChatSend()}
                        onChange={(e, p) => this.setState({ message: p.value })}
                        action={{
                            children: 'Senden',
                            basic: true,
                            onClick: this.handleChatSend,
                        }}
                    />
                </Grid.Column>
                <Grid.Column width={8} style={{ padding: '2rem' }}>
                    <div
                        style={{
                            display: 'flex',
                            flexDirection: 'column',
                            justifyContent: 'space-between',
                            height: '100%',
                            background: 'rgba(0,0,0,0.05)',
                        }}
                    >
                        <div id={'questionContainer'}>
                            <h1 id={'questionNumber'}>Q1</h1>
                            <p id={'question'}>{questionText}</p>
                        </div>
                        <div id={'answersContainer'}>
                            <List
                                selection={gameState !== 'questionsResult'}
                                divided
                                verticalAlign='middle'
                                size={'huge'}
                            >
                                {selectionArray.map((entry, entryIndex) => {
                                    return (
                                        <Answer
                                            key={entryIndex}
                                            active={entryIndex === this.state.selectedItemIndex}
                                            char={abc[entryIndex]}
                                            answer={entry.content}
                                            isResult={gameState === 'questionsResult'}
                                            correct={entry.correct || false}
                                            selectedBy={entry.selectedBy || []}
                                            clickHandler={() => this.handleSelect(entryIndex, entry.content)}
                                        />
                                    )
                                })}
                                {gameState !== 'questionsResult' && (
                                    <List.Item id={'answerSubmitButton'} onClick={this.handleSubmit}>
                                        {gameState === 'topicSelect' ? 'Auswählen' : 'Beantworten'}
                                    </List.Item>
                                )}
                            </List>
                        </div>
                    </div>
                </Grid.Column>
            </Grid>
        )
    }
}

const Answer = ({ char, answer, active, clickHandler, isResult, correct, selectedBy }) => {
    let questionResultClass = ''

    if (isResult) {
        questionResultClass = ' ' + correct
    }

    const selBy = selectedBy.map(user => (
        <Label key={user} basic size={'mini'}>
            {user}
        </Label>
    ))

    return (
        <List.Item className={'answer' + questionResultClass} active={active} onClick={clickHandler}>
            <Label basic circular className={'answerBubble'}>
                {char}
            </Label>
            {answer} {selBy}
        </List.Item>
    )
}
