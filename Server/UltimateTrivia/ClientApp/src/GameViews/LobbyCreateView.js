import React, { Component } from "react";
import { Input, Grid } from "semantic-ui-react";

export default class LobbyCreateView extends Component {
  render() {
    const { lobbyId, createLobby, joinLobby, handleInputChange } = this.props;
    return (
      <Grid columns={2} divided id={"gameView"}>
        <Grid.Column
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: "5%",
          }}
          id={"createColumn"}
          onClick={createLobby}
        >
          <div style={{ height: "auto" }}>
            <h1>Spiel erstellen</h1>
            <p>Erstelle ein Spiel dem deine Freunde beitreten können</p>
            <h1 className="clickToAction">Klicken zum Erstellen</h1>
          </div>
        </Grid.Column>
        <Grid.Column
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: "5%",
          }}
          id={"joinColumn"}
          onClick={joinLobby}
        >
          <div style={{ height: "auto" }}>
            <h1>Spiel Beitreten</h1>
            <p>
              Wenn ein Freund ein Spiel erstellt kannst du hier den Code
              eingeben und dem Spiel beitreten. <b>Viel Spaß!</b>
            </p>
            <Input
              name="lobbyId"
              value={lobbyId}
              placeholder="Code"
              onChange={(e, p) => handleInputChange(e, p, 6)}
              className={"inputUppercase"}
              style={{
                textAlign: "center",
                width: "190px",
                fontSize: "25px",
              }}
            />
            <h1 className="clickToAction">Klicken zum Beitreten</h1>
          </div>
        </Grid.Column>
      </Grid>
    );
  }
}
