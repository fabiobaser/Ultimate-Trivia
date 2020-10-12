import React, { Component } from "react";
import { Button, Card, Input } from "semantic-ui-react";

export default class JoinCreateGame extends Component {
  render() {
    const {
      connectedToLobby,
      lobbyId,
      createLobby,
      handleInputChange,
      joinLobby,
      createGame,
      leaveLobby,
    } = this.props;

    return (
      <Card.Group>
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
              onClick={createLobby}
              disabled={connectedToLobby}
            >
              Erstellen
            </Button>
          </Card.Content>
        </Card>
        <Card>
          <Card.Content>
            <Card.Header>
              Spiel
              {connectedToLobby ? " Verlassen" : " Beitreten"}
            </Card.Header>
            <Card.Description>
              Wenn ein Freund ein Spiel erstellt kannst du hier den Code
              eingeben und dem Spiel beitreten. <b>Viel Spaß!</b>
            </Card.Description>
          </Card.Content>
          <Card.Content extra>
            <Input
              name="lobbyId"
              fluid
              value={lobbyId}
              placeholder="Einladungs Code"
              onChange={handleInputChange}
              action={{
                children: connectedToLobby ? "Verlassen" : "Beitreten",
                color: connectedToLobby ? "red" : "green",
                basic: true,
                onClick: connectedToLobby ? leaveLobby : joinLobby,
              }}
            />
          </Card.Content>
        </Card>
      </Card.Group>
    );
  }
}
