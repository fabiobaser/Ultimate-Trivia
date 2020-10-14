import React, { Component } from "react";
import { Input, Image, Modal } from "semantic-ui-react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import faker from "faker";
import LobbyCreateView from "./GameViews/LobbyCreateView";
import "./App.scss";

import GameView from "./Components/GameView";

import { signinRedirect } from './Components/api-authentication/services/userService'
import { loadUserFromStorage } from './Components/api-authentication/services/userService'
import store from './redux/store';

export default class App extends Component {
  constructor(props) {
    super(props);

    this.state = {
      name: faker.name.firstName(),
      lobbyId: "",
      userArray: [],
      connectedToLobby: false,
      showSelectableQuestions: false,
      selectableQuestions: [],
      possibleAnswers: [],
      nameModalOpen: false,
      chat: [],
      inGame: false,
      gameState: "initial", //"initial", "lobby"
      topics: [],
      question: "",
      points: {},
      results: [],
    };
  }

  componentDidMount() {
    this.connectToHub();
    window.rapp = this;
  }

  sendMessage = (message) => {
    this.connection.invoke("SendMessage", message);
  };

  createGame = () => {
    this.connection.invoke("StartGame", { rounds: 3, roundDuration: 30 });
  };

  createLobby = () => {
    this.connection.invoke("CreateLobby", this.state.name);
  };

  joinLobby = () => {
    const { name, lobbyId } = this.state;
    console.log("%cConnecting to code: ", "color: orange", lobbyId);
    this.connection.invoke("JoinLobby", name, lobbyId);
  };

  leaveLobby = () => {
    this.pushtToChat("", "You left the lobby");
    this.connection.invoke("LeaveLobby");
    this.setState({ chat: [], gameState: "initial" });
  };

  pushtToChat = (sender, message) => {
    const newEntry = { sender, message };
    const chat = this.state.chat;
    chat.push(newEntry);

    this.setState({ chat: chat });
  };

  copyLobbyId = (lobbyId = this.state.lobbyId) => {
    const copyToClipboard = (str) => {
      const el = document.createElement("textarea");
      el.value = str;
      document.body.appendChild(el);
      el.select();
      document.execCommand("copy");
      document.body.removeChild(el);
    };

    copyToClipboard(lobbyId);
  };

  connectToHub = () => {
    this.connection = new HubConnectionBuilder()
      .withUrl("https://marceljenner.com:5001/triviaGameServer")
      .build();

    this.connection.on("broadcastMessage", (username, message) => {
      console.log(username, message);
      this.pushtToChat(username, message);
    });

    this.connection.on("joinLobby", (joinLobbyEvent) => {
      this.pushtToChat("", "Du bist dem Spiel beigetreten");

      //this.copyLobbyId(joinLobbyEvent.lobbyId);

      this.setState({
        userArray: joinLobbyEvent.usernames,
        connectedToLobby: true,
        lobbyId: joinLobbyEvent.lobbyId,
        inGame: false,
        gameState: "lobby",
      });
    });

    this.connection.on("userJoinedLobby", (userJoinedEvent) => {
      this.pushtToChat(
        "",
        `${userJoinedEvent.newUser} ist dem Spiel beigetreten`
      );

      this.setState({ userArray: userJoinedEvent.usernames });
    });

    this.connection.on("leaveLobby", () => {
      console.log(`You have left the lobby`);
      this.setState({ connectedToLobby: false, userArray: [] });
    });

    this.connection.on("userLeftLobby", (userLeftEvent) => {
      console.log(`${userLeftEvent.leavingUser} has left the lobby`);
      console.log("In the lobby are: ", userLeftEvent.usernames);

      this.setState({ userArray: userLeftEvent.usernames });
    });

    this.connection.on("gameStarted", () => {
      this.setState({ inGame: true });
    });

    this.connection.on("categoryChoosen", ({ username, category }) => {
      this.pushtToChat("", `${username} hat "${category}" ausgewählt`);
    });

    this.connection.on("showCategories", (showCategoriesEvent) => {
      console.log(
        `user ${showCategoriesEvent.username} is choosing a category, ${showCategoriesEvent.categories}`
      );

      this.pushtToChat(
        "",
        `${showCategoriesEvent.username} wählt ein Thema aus`
      );

      if (showCategoriesEvent.username === this.state.name) {
        this.setState({
          topics: showCategoriesEvent.categories,
          gameState: "topicSelect",
        });
      }

      this.setState({ question: "", possibleAnswers: [] });
    });

    this.connection.on("showQuestion", (showQuestionEvent) => {
      console.log("Question", showQuestionEvent);
      this.setState({
        possibleAnswers: showQuestionEvent.answers,
        question: showQuestionEvent.question,
        gameState: "question",
      });
    });

    // TODO: handle correctly
    this.connection.on("userAnswered", (userAnsweredEvent) => {
      console.log(`${userAnsweredEvent.username} has answered`);
    });

    // TODO: handle correctly
    this.connection.on(
      "highlightCorrectAnswer",
      (highlightCorrectAnswerEvent) => {
        this.setState({
          gameState: "questionsResult",
          results: highlightCorrectAnswerEvent.answers,
        });
      }
    );

    this.connection.on("updatePoints", (updatePointsEvent) => {
      this.setState({ points: updatePointsEvent.points });
    });

    this.connection.on("showFinalResult", (showFinalResultEvent) => {
      console.log("Final result: ", showFinalResultEvent.points);
      this.pushtToChat("", "Spiel zuende");
    });

    // TODO: handle correctly
    this.connection.on("error", (errorEvent) => {
      if (errorEvent.errorCode === "DUPLICATE_USERNAME") {
        console.error("username already taken");
        console.error(errorEvent.errorMessage);
      } else {
        console.error(errorEvent.errorCode + " " + errorEvent.errorMessage);
      }
    });

    this.connection
      .start()
      .then((result) => {
        console.log(result);
      })
      .catch((error) => {
        console.error(error);
      });
  };

  handleInputChange = (e, props, maxLength = 100) => {
    const { name, value } = props;
    if (this.state.connectedToLobby) return;
    if (value.length > maxLength) return;
    const newState = this.state;
    newState[name] = value;
    this.setState(newState);
  };

  handleAnswerSelect = (answer) => {
    this.connection.invoke("AnswerSelected", answer);
    //this.setState({ possibleAnswers: [] });
  };

  handleTopicSelect = (topic) => {
    this.connection.invoke("CategorySelected", topic);
    this.setState({ topics: [] });
  };

  closeModal = () => {
    this.setState({ nameModalOpen: false });
  };

  
  
  // AUTHENTICATION TEST PLAYGROUND
   
  login = () => {
    signinRedirect()
  }
  
  callApi = () => {
    
    loadUserFromStorage(store).then((user) => {
      console.log(user);

      fetch("https://marceljenner.com:5001/debug", {
        headers: {
          "Authentication": "Bearer " + user.access_token
        }
      })
    })   
  }

  // ----------------------
  
  render() {
    const {
      lobbyId,
      name,
      chat,
      userArray,
      gameState,
      topics,
      question,
      possibleAnswers,
      results,
      points,
    } = this.state;

    return (
      <div id={"appContainer"} style={{ display: "flex" }}>
        <button onClick={() => this.login()}>Login</button>
        <button onClick={() => this.callApi()}>Call api</button>
        <div
          id={"navBar"}
          style={{
            background: "white",
            height: "100vh",
            width: "70px",
            display: "flex",
            flexDirection: "column",
          }}
        >
          <h1 id={"navBarLogo"}>UT</h1>
          <div style={{ flex: 1, alignItems: "center" }}></div>
          <div style={{ textAlign: "center", paddingBottom: "1rem" }}>
            <Modal
              closeIcon
              size={"tiny"}
              onOpen={() => this.setState({ nameModalOpen: true })}
              onClose={() => this.setState({ nameModalOpen: false })}
              open={this.state.nameModalOpen}
              trigger={
                <Image
                  style={{ width: "3rem", height: "3rem" }}
                  src={`https://avatar.tobi.sh/${name}.svg?text=${name
                    .slice(0, 2)
                    .toUpperCase()}`}
                  avatar
                />
              }
            >
              <Modal.Header>Spieler Einstellungen</Modal.Header>
              <Modal.Content image>
                <Image
                  style={{ width: "10rem", height: "10rem" }}
                  src={`https://avatar.tobi.sh/${name}.svg?text=${name
                    .slice(0, 2)
                    .toUpperCase()}`}
                  avatar
                />
                <Modal.Description>
                  <Input
                    name={"name"}
                    label={{ children: "Benutzername", basic: true }}
                    value={name}
                    onChange={this.handleInputChange}
                  />
                </Modal.Description>
              </Modal.Content>
            </Modal>
          </div>
        </div>
        <div
          id={"backdropView"}
          style={{ flex: 1, background: "rgba(229, 233, 236, 1.00)" }}
        >
          {this.state.gameState === "initial" && (
            <LobbyCreateView
              lobbyId={this.state.lobbyId}
              createLobby={this.createLobby}
              joinLobby={this.joinLobby}
              handleInputChange={this.handleInputChange}
            />
          )}

          {["lobby", "topicSelect", "question", "questionsResult"].includes(
            gameState
          ) && (
            <GameView
              chat={chat}
              sendMessage={this.sendMessage}
              userArray={userArray}
              lobbyId={lobbyId}
              leaveLobby={this.leaveLobby}
              copyLobbyId={this.copyLobbyId}
              createGame={this.createGame}
              gameState={gameState}
              topics={topics}
              question={question}
              results={results}
              possibleAnswers={possibleAnswers}
              handleTopicSelect={this.handleTopicSelect}
              handleAnswerSelect={this.handleAnswerSelect}
              points={points}
            />
          )}
        </div>
      </div>
    );
  }
}
