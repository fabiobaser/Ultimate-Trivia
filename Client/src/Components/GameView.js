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
import _ from "lodash";

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
    const { handleTopicSelect, handleAnswerSelect, gameState } = this.props;
    const { selectedItem } = this.state;

    if (selectedItem === "") return;

    switch (gameState) {
      case "topicSelect":
        handleTopicSelect(selectedItem);
        break;

      case "question":
        handleAnswerSelect(selectedItem);
        break;
    }

    this.setState({ selectedItemIndex: -1, selectedItem: "" });
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
      points,
      question,
      possibleAnswers,
      results,
    } = this.props;

    let questionText = "";
    let selectionArray = [];

    switch (gameState) {
      case "topicSelect":
        selectionArray = topics.map((a) => ({ content: a }));
        questionText = "Bitte wähle eine Kategorie für die nächste Runde aus";
        break;

      case "questionsResult":
        questionText = question;
        selectionArray = results;
        break;

      case "question":
        questionText = question;
        selectionArray = possibleAnswers.map((a) => ({ content: a }));
        break;
    }

    let userPointsArray = userArray.map((user) => ({
      username: user,
      points: points[user] || 0,
    }));

    userPointsArray = _.sortBy(userPointsArray, (u) => u.points).reverse();

    console.log("SelectionArray: ", selectionArray);

    return (
      <Grid columns={3} divided id={"gameView"}>
        <Grid.Column
          width={4}
          style={{ display: "flex", flexDirection: "column", padding: "2rem" }}
        >
          <h1>Spieler</h1>
          <List ordered style={{ flex: 1 }}>
            {userPointsArray.map((userObj) => {
              return (
                <List.Item key={userObj.username}>
                  <Image
                    style={{ width: "2rem", height: "2rem" }}
                    src={`https://avatar.tobi.sh/${
                      userObj.username
                    }.svg?text=${userObj.username.slice(0, 2).toUpperCase()}`}
                    avatar
                  />
                  <List.Content>
                    <List.Header>{userObj.username}</List.Header>
                    {(userObj.points || 0) * 10} Punkte
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
          {gameState === "lobby" && (
            <Button
              fluid
              color={"green"}
              content="Starten"
              onClick={createGame}
            />
          )}
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
            onKeyDown={(e) => e.which === 13 && this.handleChatSend()}
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
                      answer={entry.content}
                      isResult={gameState === "questionsResult"}
                      correct={entry.correct || false}
                      selectedBy={entry.selectedBy || []}
                      clickHandler={() =>
                        this.handleSelect(entryIndex, entry.content)
                      }
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

const Answer = ({
  char,
  answer,
  active,
  clickHandler,
  isResult,
  correct,
  selectedBy,
}) => {
  let bubbleColor = "grey";

  if (isResult) {
    bubbleColor = correct ? "green" : "red";
  }

  const selBy = selectedBy.map((user) => (
    <Label key={user} basic size={"mini"}>
      {user}
    </Label>
  ));

  console.log("sel", selectedBy);

  return (
    <List.Item className={"answer"} active={active} onClick={clickHandler}>
      <Label circular className={"answerBubble"} color={bubbleColor}>
        {char}
      </Label>
      {answer} {selBy}
    </List.Item>
  );
};
