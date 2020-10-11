import React, { Component } from "react"
import { Input, Button } from "semantic-ui-react"
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
      <div>
        <Input
          name='name'
          placeholder='Dein Name'
          onChange={this.handleInputChange}
        />
        <Input
          name='lobbyId'
          placeholder='Einladungs Code'
          onChange={this.handleInputChange}
        />
        <Button onClick={this.createLobby}>Lobby erstellen</Button>
        <Button onClick={this.joinLobby}>Spiel beitreten</Button>
      </div>
    )
  }
}
