import React, { Component } from "react";
import {
  Grid,
  Input,
  Image,
  Feed,
  Label,
  List,
  Button,
} from "semantic-ui-react";

import "../App.scss";

const abc = "ABCD";

export default class GameView extends Component {
  state = {
    message: "",
    selectedItemIndex: -1,
    selectedItem: "",
  };

  handleChatSend = () => {
    const { message } = this.state;
    const { sendMessage } = this.props;
    sendMessage(message);
    this.setState({ message: "" });
  };

  handleSelect = (entryIndex, entry) => {
    this.setState({ selectedItemIndex: entryIndex, selectedItem: entry });
  };

  handleSubmit = () => {
    const { handleTopicSelect } = this.props;
    const { entry } = this.state;
    handleTopicSelect(entry);
  };

  render() {
    const { message } = this.state;
    const {
      chat,
      userArray,
      lobbyId,
      copyLobbyId,
      leaveLobby,
      createGame,
      gameState,
      topics,
    } = this.props;

    let questionText = "";
    let selectionArray = [];
    if (gameState === "topicSelect") {
      selectionArray = topics;
      questionText = "Bitte wähle eine Kategorie für die nächste Runde aus";
    }

    return (
      <Grid columns={3} divided id={"gameView"}>
        <Grid.Column
          width={4}
          style={{ display: "flex", flexDirection: "column", padding: "2rem" }}
        >
          <h1>Spieler</h1>
          <List ordered style={{ flex: 1 }}>
            {userArray.map((username) => {
              return (
                <List.Item key={username}>
                  <Image
                    style={{ width: "2rem", height: "2rem" }}
                    src={`https://avatar.tobi.sh/${username}.svg?text=${username
                      .slice(0, 2)
                      .toUpperCase()}`}
                    avatar
                  />
                  <List.Content>
                    <List.Header>{username}</List.Header>
                    100 Punkte
                  </List.Content>
                </List.Item>
              );
            })}
          </List>
          <Button.Group fluid>
            <Button
              color="red"
              basic
              content="Spiel verlassen"
              onClick={leaveLobby}
            />
            <Button
              color={"black"}
              basic
              content={lobbyId}
              onClick={() => copyLobbyId()}
            />
          </Button.Group>
          <Button
            fluid
            color={"green"}
            content="Starten"
            onClick={createGame}
          />
        </Grid.Column>
        <Grid.Column
          width={4}
          style={{ display: "flex", flexDirection: "column", padding: "2rem" }}
        >
          <h1>Chat</h1>
          <Feed style={{ marginTop: "1rem", flex: 1 }}>
            {chat.map((entry, index) => {
              const { sender, message } = entry;
              return (
                <Feed.Event
                  className={sender === "" ? "systemMessage" : ""}
                  key={sender + index}
                >
                  <Feed.Label
                    image={
                      sender
                        ? `https://avatar.tobi.sh/${sender}.svg?text=${sender
                            .slice(0, 2)
                            .toUpperCase()}`
                        : ""
                    }
                  />
                  <Feed.Content date={sender} summary={message} />
                </Feed.Event>
              );
            })}
          </Feed>
          <Input
            fluid
            name={"message"}
            value={message}
            onKeyDown={(e, b) => {
              console.log(e.keycode, b);
            }}
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
              <p id={"question"}>{questionText}</p>
            </div>
            <div id={"answersContainer"}>
              <List selection divided verticalAlign="middle" size={"huge"}>
                {selectionArray.map((entry, entryIndex) => {
                  return (
                    <Answer
                      key={entryIndex}
                      active={entryIndex === this.state.selectedItemIndex}
                      char={abc[entryIndex]}
                      answer={entry}
                      clickHandler={() => this.handleSelect(entryIndex, entry)}
                    />
                  );
                })}
                <List.Item
                  id={"answerSubmitButton"}
                  onClick={this.handleSubmit}
                >
                  {gameState === "topicSelect" ? "Auswählen" : "Beantworten"}
                </List.Item>
              </List>
            </div>
          </div>
        </Grid.Column>
      </Grid>
    );
  }
}

const Answer = ({ char, answer, active, clickHandler }) => {
  return (
    <List.Item className={"answer"} active={active} onClick={clickHandler}>
      <Label circular className={"answerBubble"}>
        {char}
      </Label>
      {answer}
    </List.Item>
  );
};
