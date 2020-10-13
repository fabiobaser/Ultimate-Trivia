import React, { Component } from "react";
import {
  Card,
  Grid,
  Label,
  List,
  Feed,
  Button,
  Image,
} from "semantic-ui-react";
import SelectButton from "./SelectButton";
import _ from "lodash";

export default class Game extends Component {
  render() {
    const {
      handleAnswerSelect,
      possibleAnswers,
      userArray,
      points,
      name,
      chat,
      createGame,
      leaveLobby,
      topics,
      handleTopicSelect,
      question,
      inGame,
    } = this.props;

    console.log(points);

    return (
      <Grid celled="internally">
        <Grid.Row>
          <Grid.Column width={4}>
            {userArray.length > 0 && (
              <Card>
                <Card.Content>
                  <Card.Header>Spieler</Card.Header>
                  <List>
                    {userArray.map((user, userIndex) => (
                      <List.Item key={user}>
                        <Label className="fluid" basic={name === user}>
                          {userIndex + 1 + "."}
                          <Image
                            src={`https://avatar.tobi.sh/${name}.svg?text=${name
                              .slice(0, 2)
                              .toUpperCase()}`}
                            avatar
                            style={{
                              marginRight: "0.5rem",
                              marginLeft: "0.5rem",
                            }}
                          />
                          {user}
                          <Label.Detail
                            style={{ float: "right", lineHeight: "26px" }}
                          >
                            {(points || {})[user] || 0}
                          </Label.Detail>
                        </Label>
                      </List.Item>
                    ))}
                  </List>
                </Card.Content>
              </Card>
            )}
          </Grid.Column>
          <Grid.Column width={8}>
            {topics.length > 0 && (
              <div>
                <h2>Wähle eine Frage aus</h2>
                {topics.map((t) => (
                  <SelectButton key={t} value={t} handler={handleTopicSelect} />
                ))}
              </div>
            )}

            {possibleAnswers.length > 0 && (
              <div>
                <h2>{question}</h2>
                {possibleAnswers.map((a) => (
                  <SelectButton
                    key={a}
                    value={a}
                    handler={handleAnswerSelect}
                  />
                ))}
              </div>
            )}
          </Grid.Column>
          <Grid.Column width={4}>
            <Button.Group>
              {!inGame && (
                <Button basic color={"green"} onClick={createGame}>
                  Start
                </Button>
              )}
              <Button basic color={"red"} onClick={leaveLobby}>
                Spiel verlassen
              </Button>
            </Button.Group>

            <Feed>
              {chat.map((post, postIndex) => {
                return (
                  <Feed.Event key={postIndex}>
                    <Feed.Content>
                      <Feed.Date>{post.sender}</Feed.Date>
                      <Feed.Summary>{post.message}</Feed.Summary>
                    </Feed.Content>
                  </Feed.Event>
                );
              })}
            </Feed>
          </Grid.Column>
        </Grid.Row>
      </Grid>
    );
  }
}
