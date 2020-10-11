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

  connectToHub = () => {
    const connection = new HubConnectionBuilder().withUrl("/").build()

    connection.on("send", (data) => {
      console.log(data)
    })

    connection.start().then(() => connection.invoke("send", "Hello"))
  }

  handleInputChange = (e, props) => {
    const { name, value } = props
    const newState = this.state
    newState[name] = value
    this.setState(newState)
  }

  connectToLobby = () => {
    const { lobbyId } = this.state
    console.log("%cConnecting to code: ", "color: orange", lobbyId)
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
        <Button onClick={this.connectToLobby}>Spiel beitreten</Button>
      </div>
    )
  }
}
