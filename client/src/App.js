import React, { Component } from "react"
import { Input, Button, Card, Container, Header } from "semantic-ui-react"
import { HubConnectionBuilder } from "@microsoft/signalr"

export default class App extends Component {
  constructor(props) {
    super(props)

    this.state = {
      name: "",
      lobbyId: "",
    }
  }

  componentDidMount() {
    this.connectToHub()
  }

  createLobby = () => {
    this.connection.invoke("CreateLobby", this.state.name)
  }

  joinLobby = () => {
    const { name, lobbyId } = this.state
    console.log("%cConnecting to code: ", "color: orange", lobbyId)
    this.connection.invoke("JoinLobby", name, lobbyId)
  }

  connectToHub = () => {
    this.connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5000/triviaGameServer")
      .build()

    this.connection.on("broadcastMessage", (username, message) => {
      console.log(username, message)
    })

    this.connection
      .start()
      .then(() => this.connection.invoke("Send", "Hello", "wurst"))
  }

  handleInputChange = (e, props) => {
    const { name, value } = props
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
          placeholder='Dein Name'
          onChange={this.handleInputChange}
        />

        {this.state.name !== "" && (
          <Card.Group centered style={{ marginTop: "2rem" }} disabled>
            <Card>
              <Card.Content>
                <Card.Header>Spiel erstellen</Card.Header>
                <Card.Description>
                  Erstelle ein Spiel dem deine Freunde beitreten können
                </Card.Description>
              </Card.Content>
              <Card.Content extra>
                <Button basic fluid onClick={this.createLobby}>
                  Erstellen
                </Button>
              </Card.Content>
            </Card>

            <Card>
              <Card.Content>
                <Card.Header>Spiel beitreten</Card.Header>
                <Card.Description>
                  Wenn ein Freund ein Spiel erstellt kannst du hier den Code
                  eingeben und dem Spiel beitreten. <b>Viel Spaß!</b>
                </Card.Description>
              </Card.Content>
              <Card.Content extra>
                <Input
                  name='lobbyId'
                  fluid
                  placeholder='Einladungs Code'
                  onChange={this.handleInputChange}
                  action={{
                    children: "Beitreten",
                    color: "green",
                    basic: true,
                    onClick: this.joinLobby,
                  }}
                />
              </Card.Content>
            </Card>
          </Card.Group>
        )}
      </Container>
    )
  }
}
