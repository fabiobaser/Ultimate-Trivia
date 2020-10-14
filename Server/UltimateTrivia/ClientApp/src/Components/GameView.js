import React, { Component } from "react";
import { Grid, Input, Feed, Label, List } from "semantic-ui-react";

import "../App.scss";

export default class AppAlt extends Component {
  state = {
    message: "wef",
  };

  handleChatSend = () => {
    const { message } = this.state;
    const { sendMessage } = this.props;
    sendMessage(message);
  };

  render() {
    const { message } = this.state;
    const { chat } = this.props;

    return (
      <Grid columns={3} divided id={"gameView"}>
        <Grid.Column width={4}>
          <h1>Spieler</h1>
        </Grid.Column>
        <Grid.Column
          width={4}
          style={{ display: "flex", flexDirection: "column" }}
        >
          <h1>Chat</h1>
          <Feed style={{ marginTop: "3rem", flex: 1 }}>
            {chat.map((entry, index) => {
              const { sender, message } = entry;
              return (
                <Feed.Event
                  className={sender === "" ? "systemMessage" : ""}
                  key={sender + index}
                  date={sender}
                  summary={message}
                />
              );
            })}
          </Feed>
          <Input
            fluid
            name={"message"}
            value={message}
            onChange={(e, p) => this.setState({ message: p.value })}
            action={{
              children: "Senden",
              basic: true,
              onClick: this.handleChatSend,
            }}
          />
        </Grid.Column>
        <Grid.Column width={8} style={{ padding: "2rem" }}>
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              justifyContent: "space-between",
              height: "100%",
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
    );
  }
}
