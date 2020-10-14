import React, { Component } from "react";
import { Grid, Button, Label, List } from "semantic-ui-react";

import "./App.scss";

export default class AppAlt extends Component {
  render() {
    return (
      <div id={"appContainer"} style={{ display: "flex" }}>
        <div
          id={"navBar"}
          style={{ background: "white", height: "100vh", width: "70px" }}
        >
          Navbar
        </div>
        <div
          id={"backdropView"}
          style={{ flex: 1, background: "rgba(229, 233, 236, 1.00)" }}
        >
          <Grid columns={3} divided stretched id={"gameView"}>
            <Grid.Column width={4}>
              <h1>Spieler</h1>
            </Grid.Column>
            <Grid.Column width={4}>
              <h1>Chat</h1>
            </Grid.Column>
            <Grid.Column width={8} style={{ padding: "2rem" }}>
              <div
                style={{
                  display: "flex",
                  flexDirection: "column",
                  justifyContent: "space-between",
                  background: "rgba(0,0,0,0.05)",
                }}
              >
                <div id={"questionContainer"}>
                  <h1 id={"questionNumber"}>Q1</h1>
                  <p id={"question"}>
                    Wo findet seit 1990 das weltweit größte Zappa-Festival, die
                    Zappanale, statt?
                  </p>
                </div>
                <div id={"answersContainer"}>
                  <List selection divided verticalAlign="middle" size={"huge"}>
                    <List.Item className={"answer"}>
                      <Label circular className={"answerBubble"}>
                        A
                      </Label>
                      Bad Doberan
                    </List.Item>
                    <List.Item className={"answer"}>
                      <Label circular className={"answerBubble"}>
                        B
                      </Label>
                      Berlin
                    </List.Item>
                    <List.Item className={"answer"}>
                      <Label circular className={"answerBubble"}>
                        C
                      </Label>
                      Maryland
                    </List.Item>
                    <List.Item className={"answer"}>
                      <Label circular className={"answerBubble"}>
                        D
                      </Label>
                      San Francisco
                    </List.Item>
                    <List.Item id={"answerSubmitButton"}>Beantworten</List.Item>
                  </List>
                </div>
              </div>
            </Grid.Column>
          </Grid>
        </div>
      </div>
    );
  }
}
