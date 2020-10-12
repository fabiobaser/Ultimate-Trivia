import React, { Component } from "react"
import { Input, Button, Card, Container, Header } from "semantic-ui-react"
import { HubConnectionBuilder } from "@microsoft/signalr"

export default class App extends Component {
  constructor(props) {
    super(props)

    this.state = {
      name: "Bobby",
      lobbyId: "",
      userArray: [],
      connectedToLobby: false,
    }
  }

  componentDidMount() {
    this.connectToHub()
  }

  createGame = () => {
    this.connection.invoke("CreateGame", { rounds: 3, roundDuration: 30 })
  }
  
  createLobby = () => {
    this.connection.invoke("CreateLobby", this.state.name)
  }

  joinLobby = () => {
    const { name, lobbyId } = this.state
    console.log("%cConnecting to code: ", "color: orange", lobbyId)
    this.connection.invoke("JoinLobby", name, lobbyId)
  }

  leaveLobby = () => {
    console.log("%cLeaving lobby", "color: red")
    this.connection.invoke("LeaveLobby")
  }

  connectToHub = () => {
    this.connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5000/triviaGameServer")
      .build()

    this.connection.on("broadcastMessage", (username, message) => {
      console.log(username, message)
    })

    this.connection.on("joinLobby", (joinLobbyEvent) => {
      console.log("You joined the Lobby")
      console.log(joinLobbyEvent.lobbyId, joinLobbyEvent.usernames)
      this.setState({ userArray: joinLobbyEvent.usernames, connectedToLobby: true, lobbyId: joinLobbyEvent.lobbyId })
    })

    this.connection.on("userJoinedLobby", (userJoinedEvent) => {
      console.log(`${userJoinedEvent.newUser} joined the lobby`)
      console.log("In the lobby are: ", userJoinedEvent.usernames)
      this.setState({ userArray: userJoinedEvent.usernames })
    })

    this.connection.on("leaveLobby", () => {
      console.log(`You have left the lobby`)
      this.setState({ connectedToLobby: false, userArray: [] })
    })

    this.connection.on("userLeftLobby", (userLeftEvent) => {
      console.log(`${userLeftEvent.leavingUser} has left the lobby`)
      console.log("In the lobby are: ", userLeftEvent.usernames)

      this.setState({ userArray: userLeftEvent.usernames })
    })

    
    // TODO : Handle events correctly
    this.connection.on("gameStarted", () => {
      console.log(`game started`)
    })

    this.connection.on("showCategories", (showCategoriesEvent) => {
      console.log(`user ${showCategoriesEvent.username} is choosing a category`)
      console.log(showCategoriesEvent.categories)

      if(this.state.name == showCategoriesEvent.username) {
        this.connection.invoke("CategorySelected", "Hausparty")
      }
    })

    this.connection.on("showQuestion", (showQuestionEvent) => {
      console.log(`Question: ${showQuestionEvent.question}`)
      console.log(showQuestionEvent.answers)

      this.connection.invoke("AnswerSelected", "aha")     
    })

    this.connection.on("updatePoints", (updatePointsEvent) => {
      console.log(updatePointsEvent.points)
    })

    this.connection.on("showFinalResult", (showFinalResultEvent) => {
      console.log(showFinalResultEvent.points)
    })
    
    // ----------------------------

    this.connection.start()
  }

  handleInputChange = (e, props) => {
    const { name, value } = props
    if (this.state.connectedToLobby) return
    const newState = this.state
    newState[name] = value
    this.setState(newState)
  }

  render() {
    return (
      <Container textAlign='center' style={{ paddingTop: "4rem" }}>
        <Header as='h1'>Wilkommen bei Ultimate Trivia</Header>
        <p>Gib als erstes deinen Namen ein</p>
        <Input
          name='name'
          value={this.state.name}
          placeholder='Dein Name'
          onChange={this.handleInputChange}
        />

        {this.state.name !== "" && (
          <Card.Group centered style={{ marginTop: "2rem" }}>
            {this.state.userArray.length > 0 && (
              <Card>
                <Card.Content>
                  <Card.Header>Spieler</Card.Header>
                  {this.state.userArray.map((user) => (
                    <p key={user}>{user}</p>
                  ))}
                </Card.Content>
              </Card>
            )}

            {!this.state.connectedToLobby && (
              <Card>
                <Card.Content>
                  <Card.Header>Spiel erstellen</Card.Header>
                  <Card.Description>
                    Erstelle ein Spiel dem deine Freunde beitreten können
                  </Card.Description>
                </Card.Content>
                <Card.Content extra>
                  <Button
                    basic
                    fluid
                    onClick={this.createLobby}
                    disabled={this.state.connectedToLobby}>
                    Erstellen
                  </Button>                 
                </Card.Content>
              </Card>
            )}

            <Card>
              <Card.Content>
                <Card.Header>
                  Spiel
                  {this.state.connectedToLobby ? " Verlassen" : " Beitreten"}
                </Card.Header>
                <Card.Description>
                  Wenn ein Freund ein Spiel erstellt kannst du hier den Code
                  eingeben und dem Spiel beitreten. <b>Viel Spaß!</b>
                </Card.Description>
              </Card.Content>
              <Card.Content extra>
                <Input
                  name='lobbyId'
                  fluid
                  value={this.state.lobbyId}
                  placeholder='Einladungs Code'
                  onChange={this.handleInputChange}
                  action={{
                    children: this.state.connectedToLobby
                      ? "Verlassen"
                      : "Beitreten",
                    color: this.state.connectedToLobby ? "red" : "green",
                    basic: true,
                    onClick: this.state.connectedToLobby
                      ? this.leaveLobby
                      : this.joinLobby,
                  }}
                />
                <Button
                    basic
                    fluid
                    onClick={this.createGame}
                    disabled={!this.state.connectedToLobby}>
                  Start
                </Button>
              </Card.Content>
            </Card>
          </Card.Group>
        )}
        {this.state.lobbyId !== "" && (
          <h1>Einladungs Code: {this.state.lobbyId}</h1>
        )}
      </Container>
    )
  }
}
